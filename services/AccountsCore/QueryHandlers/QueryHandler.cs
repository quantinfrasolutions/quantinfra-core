using System.Collections.Generic;
using System.Linq;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Common.Utils.Collections;
using QuantInfra.Domain.Account.Execution.State;
using QuantInfra.Domain.Accounts.Base.State;
using QuantInfra.Domain.Queries.Accounts.AccountsService;
using QuantInfra.Domain.Queries.Strategies;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Accounts.AccountStates;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading.Orders;
using QuantInfra.Sdk.Trading.Positions;
using QuantInfra.Services.AccountsCore.State;

namespace QuantInfra.Services.AccountsCore.QueryHandlers;

public record GetAccountState(int AccountId) :
    IQuery<AccountBaseState?>;


public class QueryHandler(AccountServiceState state, IQueryBus queryBus, IClock clock) :
    IQueryHandler<GetAccountState, AccountBaseState?>,
    IQueryHandler<QuantInfra.Domain.Queries.Accounts.AccountsService.GetAccountState, AccountStateReadonly?>,
    IQueryHandler<QuantInfra.Domain.Queries.Accounts.AccountsService.GetBrokerAccountState, BrokerAccountStateReadonly?>,
    IQueryHandler<GetAccountIdsForEndOfDay, IReadOnlyCollection<int>>,
    IQueryHandler<GetActiveVirtualExecutorOrders, IReadOnlyCollection<OrderStatus>>,
    IQueryHandler<GetStrategyState, IStrategyStateReadonly?>,
    IQueryHandler<global::QuantInfra.Domain.Queries.Strategies.AccountsService.GetStrategyState, StrategyStateReadonly?>,
    IQueryHandler<GetActiveOrders, IReadOnlyCollection<OrderStatus>>,
    IQueryHandler<GetPositions, IReadOnlyCollection<Position>>,
    IQueryHandler<GetBalances, IReadOnlyDictionary<int, decimal>>,
    IQueryHandler<GetBrokerAccountReconciliationStatus, BrokerAccountReconciliationStatus?>
{

    public AccountBaseState Handle(GetAccountState query) =>
        state.AccountStates[query.AccountId];
    
    public AccountStateReadonly? Handle(QuantInfra.Domain.Queries.Accounts.AccountsService.GetAccountState query) =>
        state.AccountStates.GetValueOrDefault(query.AccountId)?.ToAccountStateReadonly();
    
    BrokerAccountStateReadonly? IQueryHandler<GetBrokerAccountState, BrokerAccountStateReadonly?>.Handle(GetBrokerAccountState query)
    {
        var state1 = state.AccountStates.GetValueOrDefault(query.AccountId);
        if (state1 == null || state1 is not BrokerAccountState bs) return null;
        return bs.ToAccountStateReadonly();
    }
    
    public IReadOnlyCollection<OrderStatus> Handle(GetActiveVirtualExecutorOrders query)
    {
        var accounts = state.AccountRecords
            .Where(a => 
                (query.VirtualAccounts && a.Value.AccountType == AccountType.VirtualAccount)
                || (!query.VirtualAccounts && a.Value.AccountType != AccountType.VirtualAccount))
            .Select(a => a.Key)
            .ToHashSet();
        
        return state.AccountStates.Values
            .Where(a => accounts.Contains(a.AccountId))
            .SelectMany(s =>
                s.Orders.Where(o => o is { IsVirtual: true, IsSuspended: false })
            ).ToList();
    }

    public IStrategyStateReadonly? Handle(GetStrategyState query) => 
        state.StrategyStates.GetValueOrDefault(query.StrategyId);
    
    public StrategyStateReadonly? Handle(QuantInfra.Domain.Queries.Strategies.AccountsService.GetStrategyState query) => 
        state.StrategyStates.GetValueOrDefault(query.StrategyId)?.ToStrategyStateReadonly();

    public IReadOnlyCollection<OrderStatus> Handle(GetActiveOrders query) =>
        state.AccountStates[query.AccountId].Orders.ToList();

    public IReadOnlyCollection<int> Handle(GetAccountIdsForEndOfDay query) => state.AccountStates.Keys.ToList();
    
    public IReadOnlyCollection<Position> Handle(GetPositions query)
    {
        if (!query.MarkToMarket) return state.AccountStates[query.AccountId].Positions.ToList();
        
        var account = queryBus.Query<GetAccount, IAccount?>(new(query.AccountId));
        var mtm = account.MarkToMarket(clock.GetCurrentInstant());

        return mtm.positions;
    }

    public IReadOnlyDictionary<int, decimal> Handle(GetBalances query) =>
        state.AccountStates[query.AccountId].Balances.Copy();

    public BrokerAccountReconciliationStatus? Handle(GetBrokerAccountReconciliationStatus query)
    {
        var s = state.AccountStates[query.AccountId];
        return s is BrokerAccountState ba
            ? new BrokerAccountReconciliationStatus(ba.IsReconciled, 
                ba.ReconciliationMessages.ToList(),
                ba.UnmappedExternalContractIds.ToList(),
                ba.UnmappedExternalAssetIds.ToList()
            )
            : null;
    }
}
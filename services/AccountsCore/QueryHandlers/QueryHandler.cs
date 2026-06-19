using System.Collections.Generic;
using System.Linq;
using QuantInfra.Common.EventSourcing;
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


public class QueryHandler : 
    IQueryHandler<GetAccountState, AccountBaseState?>,
    IQueryHandler<QuantInfra.Domain.Queries.Accounts.AccountsService.GetAccountState, AccountStateReadonly?>,
    IQueryHandler<QuantInfra.Domain.Queries.Accounts.AccountsService.GetBrokerAccountState, BrokerAccountStateReadonly?>,
    IQueryHandler<GetAccountIdsForEndOfDay, IReadOnlyCollection<int>>,
    
    IQueryHandler<GetActiveVirtualExecutorOrders, IReadOnlyCollection<OrderStatus>>,
    
    IQueryHandler<GetStrategyState, IStrategyStateReadonly?>,
    IQueryHandler<global::QuantInfra.Domain.Queries.Strategies.AccountsService.GetStrategyState, StrategyStateReadonly?>,
    
    IQueryHandler<GetActiveOrders, IReadOnlyCollection<OrderStatus>>,
    IQueryHandler<GetPositions, IReadOnlyCollection<Position>>,
    
    IQueryHandler<GetBalances, IReadOnlyDictionary<int, decimal>>
{
    private readonly AccountServiceState _state;

    public QueryHandler(AccountServiceState state)
    {
        _state = state;
    }

    public AccountBaseState Handle(GetAccountState query) =>
        _state.AccountStates[query.AccountId];
    
    public AccountStateReadonly? Handle(QuantInfra.Domain.Queries.Accounts.AccountsService.GetAccountState query) =>
        _state.AccountStates.GetValueOrDefault(query.AccountId)?.ToAccountStateReadonly();
    
    BrokerAccountStateReadonly? IQueryHandler<GetBrokerAccountState, BrokerAccountStateReadonly?>.Handle(GetBrokerAccountState query)
    {
        var state = _state.AccountStates.GetValueOrDefault(query.AccountId);
        if (state == null || state is not BrokerAccountState bs) return null;
        return bs.ToAccountStateReadonly();
    }
    
    public IReadOnlyCollection<OrderStatus> Handle(GetActiveVirtualExecutorOrders query)
    {
        var accounts = _state.AccountRecords
            .Where(a => 
                (query.VirtualAccounts && a.Value.AccountType == AccountType.VirtualAccount)
                || (!query.VirtualAccounts && a.Value.AccountType != AccountType.VirtualAccount))
            .Select(a => a.Key)
            .ToHashSet();
        
        return _state.AccountStates.Values
            .Where(a => accounts.Contains(a.AccountId))
            .SelectMany(s =>
                s.Orders.Where(o => o is { IsVirtual: true, IsSuspended: false })
            ).ToList();
    }

    public IStrategyStateReadonly? Handle(GetStrategyState query) => 
        _state.StrategyStates.GetValueOrDefault(query.StrategyId);
    
    public StrategyStateReadonly? Handle(QuantInfra.Domain.Queries.Strategies.AccountsService.GetStrategyState query) => 
        _state.StrategyStates.GetValueOrDefault(query.StrategyId)?.ToStrategyStateReadonly();

    public IReadOnlyCollection<OrderStatus> Handle(GetActiveOrders query) =>
        _state.AccountStates[query.AccountId].Orders.ToList();

    public IReadOnlyCollection<int> Handle(GetAccountIdsForEndOfDay query) => _state.AccountStates.Keys.ToList();
    
    public IReadOnlyCollection<Position> Handle(GetPositions query) =>
        _state.AccountStates[query.AccountId].Positions.ToList();

    public IReadOnlyDictionary<int, decimal> Handle(GetBalances query) =>
        _state.AccountStates[query.AccountId].Balances.Copy();
}
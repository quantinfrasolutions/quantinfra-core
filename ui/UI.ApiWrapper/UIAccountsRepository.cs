using NodaTime;
using NodaTime.Text;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Trading.Orders;
using UI.Interfaces.Accounts;

namespace UI.ApiWrapper;

public partial class ApiRepository : IUiAccountsRepository
{
    public Task<Dictionary<int, AccountListModel>> GetBrokerAccounts(bool refresh = false)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<int, AccountListModel>> GetAccounts(bool refresh = false)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<AccountListModel>> GetAccounts(AccountsFilter filter) =>
        RetrieveCollection("accounts", () => _wrapper.Client.GetAccountsAsync(
            filter.AccountIds, 
            filter.AccountName, 
            filter.AccountTypes.Select(a => (int)a),
            filter.StrategyId,
            CancellationToken.None
        ));

    public Task CreateAccount(CreateAccountRequest account) =>
        Call("Account created", "Could not create account", () => _wrapper.Client.CreateAccountAsync(account));
    

    public Task<IEnumerable<BalanceOperationHistoryModel>> GetBalanceOperations(BalanceOperationsFilter filter) =>
        RetrieveCollection("balance operations", () => _wrapper.Client.GetBalanceOperationsHistoryAsync(filter.AccountId,
            filter.BalanceOperationId, filter.FromDt, filter.ToDt, filter.ExternalId,
            filter.Limit, filter.Offset));

    public Task CreateBalanceOperation(NewBalanceOperation request) =>
        Call("balance operation created", "Failed",
            () => _wrapper.Client.CreateBalanceOperationAsync(request.AccountId, request));

    public Task<AccountListModel> GetAccount(int accountId) =>
        Retrieve("account", () => _wrapper.Client.GetAccountAsync(accountId));
        // (await GetAccounts(new AccountsFilter { AccountIds = [accountId] })).SingleOrDefault();

    public Task<IEnumerable<BalanceModel>> GetBalances(int accountId) =>
        RetrieveCollection("balances", () => _wrapper.Client.GetBalancesAsync(accountId));

    public Task<IEnumerable<OrderView>> GetActiveOrders(int accountId) =>
        RetrieveCollection("orders",
            () => _wrapper.Client.GetActiveOrdersAsync(accountId, CancellationToken.None)
        );

    public Task<IEnumerable<OrderHistoryView>> GetOrdersHistory(OrderFilter filter) =>
        RetrieveCollection("orders history",
            () => _wrapper.Client.GetOrdersHistoryAsync(
                filter.AccountId,
                filter.OrderId,
                filter.ContractId,
                (int?)filter.OrdStatus,
                filter.ExternalId,
                filter.ExecutionRequestId,
                filter.FromDt, 
                filter.ToDt,
                (int?)filter.ExecType,   
                filter.Limit, filter.Offset)
        );

    Task<IEnumerable<PositionView>> IUiAccountsRepository.GetActivePositions(int accountId) =>
        RetrieveCollection("positions",
            () => _wrapper.Client.GetActivePositionsAsync(accountId, CancellationToken.None)
        );
    // RetrieveCollection("orders history",
        //     () => _wrapper.Client.GetOrdersHistoryAsync(InstantToString(filter.FromDt), InstantToString(filter.ToDt), 
        //         filter.ExecType.ToString(), filter.AccountId,
        //         filter.OrderId, filter.ContractId, filter.OrdStatus.ToString(), filter.ExternalId, filter.ExecutionRequestId)
        // );


        public Task<IEnumerable<PositionView>> GetPositionsHistory(PositionHistoryFilter? filter = null) =>
            RetrieveCollection("positions history", () => _wrapper.Client.GetPositionsHistoryAsync(
                filter?.CloseDtFrom,
                filter?.CloseDtTo,
                filter?.Type?.Select(i => (int)i),
                filter?.OpenDtFrom,
                filter?.OpenDtTo,
                filter?.HistoryOpenDtFrom,
                filter?.HistoryOpenDtTo,
                filter?.AccountId,
                filter?.ContractId,
                filter?.TradeId
            ));

    public Task<IEnumerable<TradeView>> GetTradesHistory(TradeFilter filter) =>
        RetrieveCollection("trades history",
            () => _wrapper.Client.GetTradesHistoryAsync(
                filter.FromDt, 
                filter.ToDt, 
                filter.AccountId, filter.TradeId, filter.ContractId, filter.ExternalId, 
                filter.Limit, filter.Offset
            )
        );

    public Task<IEnumerable<SharePriceHistory>> GetSharePriceHistory(int accountId) =>
        RetrieveCollection("share price history",
            () => _wrapper.Client.GetAccountSharePriceHistoryAsync(accountId)
        );

    public Task SendNewOrder(NewOrderSingle nos) =>
        Call("Order created", "Failed", (() => _wrapper.Client.NewOrderAsync(nos)));

    public Task CancelOrder(OrderCancelRequest ocr) =>
        Call("Order cancellation requested", "Failed", () => _wrapper.Client.CancelOrderAsync(ocr));

    public Task<IEnumerable<SubaccountListModel>> GetSubAccounts(SubaccountsFilter filter)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<SubaccountListModel>> GetBrokerAccountsForSsa(int accountId) =>
        RetrieveCollection("broker subaccounts", 
            () => _wrapper.Client.GetSubaccountsAsync(accountId, SubaccountType.Broker.ToString())
        );

    public Task AssignBrokerAccountToSsa(int accountId, AssignSsaToBrokerAccountRequest request) =>
        throw new NotImplementedException();
        // Call("Broker account assigned", "Failed",
        //     () => _wrapper.Client.AssignBrokerAccountToSsaAsync(accountId, request));
        
    // public Task<IEnumerable<FitnessTestResult>> GetMetrics(Guid accountId) =>
    //     RetrieveCollection("metrics", () => _wrapper.Client.GetMetricsAsync(accountId));
    

    public Task CreateSubaccount(CreateSubaccountRequest r) =>
        Call("subaccount created", "failed",
            () => _wrapper.Client.CreateSubaccountAsync(r.AccountId, r)
        );

    public Task<IEnumerable<SharePriceHistory>> GetSharePriceHistory(SharePriceHistoryFilter filter) =>
        RetrieveCollection("share price history",
            () => _wrapper.Client.GetSharePriceHistoryAsync(filter.AccountId, filter.SortDescending, 
                filter.FromDt, filter.ToDt, 
                (int?)filter.ChangeType, filter.Limit, filter.Offset)
        );

    public Task CreateTradingAccountConfig(CreateTradingClientConfigRequest data) =>
        Call("Configuration created", "Error", () => _wrapper.Client.CreateTradingClientConfigAsync(data.AccountId, data));

    public Task DeleteTradingAccountConfig(int accountId) =>
        Call("Configuration deleted", "Error", () => _wrapper.Client.DeleteTradingClientConfigAsync(accountId));

    private string? InstantToString(Instant? i) =>
        i.HasValue ? InstantPattern.ExtendedIso.Format(i.Value) : null;
}
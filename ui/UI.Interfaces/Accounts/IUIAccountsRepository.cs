using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Trading.Orders;

namespace UI.Interfaces.Accounts;

public interface IUiAccountsRepository
{
    public Task<Dictionary<int, AccountListModel>> GetBrokerAccounts(bool refresh = false);
    public Task<Dictionary<int, AccountListModel>> GetAccounts(bool refresh = false);
    public Task<IEnumerable<AccountListModel>> GetAccounts(AccountsFilter filter);
    public Task CreateAccount(CreateAccountRequest account);
    public Task<BrokerAccountReconciliationStatus?> GetBrokerAccountReconciliationStatus(int accountId);
    public Task Reconcile(int accountId);
    Task<IEnumerable<BalanceOperationHistoryModel>> GetBalanceOperations(BalanceOperationsFilter filter);
    Task CreateBalanceOperation(NewBalanceOperation request);
    Task<AccountListModel> GetAccount(int accountId);
    Task<IEnumerable<BalanceModel>> GetBalances(int accountId);
    Task<IEnumerable<OrderView>> GetActiveOrders(int accountId);
    Task<IEnumerable<OrderHistoryView>> GetOrdersHistory(OrderFilter filter);
    Task<IEnumerable<PositionView>> GetActivePositions(int accountId);
    Task<IEnumerable<PositionView>> GetPositionsHistory(PositionHistoryFilter? filter);
    Task<IEnumerable<TradeView>> GetTradesHistory(TradeFilter filter);
    Task<IEnumerable<SharePriceHistory>> GetSharePriceHistory(int accountId);
    Task SendNewOrder(NewOrderSingle nos);
    Task CancelOrder(OrderCancelRequest ocr);
    Task<IEnumerable<SubaccountListModel>> GetSubAccounts(SubaccountsFilter filter);
    Task<IEnumerable<SubaccountListModel>> GetBrokerAccountsForSsa(int accountId);
    Task AssignBrokerAccountToSsa(int accountId, AssignSsaToBrokerAccountRequest request);
    Task CreateSubaccount(CreateSubaccountRequest r);
    Task<IEnumerable<SharePriceHistory>> GetSharePriceHistory(SharePriceHistoryFilter filter);
    Task CreateTradingAccountConfig(CreateTradingClientConfigRequest data);
    Task DeleteTradingAccountConfig(int accountId);
}
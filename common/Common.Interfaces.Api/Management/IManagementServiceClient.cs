using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Common.Interfaces.Api.Strategies;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Trading.Orders;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Common.Interfaces.Api.Management;

public interface IManagementServiceClient
{
    Task<int> CreateAccountAsync(CreateAccountRequest request);
    Task CreateSubaccountAsync(CreateSubaccountRequest request);
    Task<IReadOnlyDictionary<int, decimal>> GetBalancesAsync(int accountId);
    Task<int> CreateBalanceOperationAsync(NewBalanceOperation request);
    Task<IReadOnlyCollection<Position>> GetActivePositionsAsync(int accountId);
    Task<IReadOnlyCollection<OrderStatus>> GetActiveOrdersAsync(int accountId);
    Task PlaceOrderAsync(NewOrderSingle nos);
    Task CancelOrderAsync(OrderCancelRequest ocr);
    Task CreateStrategyAsync(CreateStrategyRequest request);
    Task StartStrategyAsync(int strategyId);
    Task CreateTradingClientConfig(TradingClientConfig config);
    Task DeleteTradingClientConfig(int accountId);
}

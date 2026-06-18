using Common.Accounts.Abstractions;
using Common.Trading.Positions;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Common.Interfaces.Api.Strategies;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Common.Interfaces.Api.Management;

public interface IManagementServiceClient
{
    Task CreateAccountAsync(CreateAccountRequest request);
    Task CreateSubaccountAsync(CreateSubaccountRequest request);
    Task<IReadOnlyDictionary<int, decimal>> GetBalancesAsync(int accountId);
    Task CreateBalanceOperationAsync(NewBalanceOperation request);
    Task<IReadOnlyCollection<Position>> GetActivePositionsAsync(int accountId);
    Task<IReadOnlyCollection<OrderStatus>> GetActiveOrdersAsync(int accountId);
    Task PlaceOrderAsync(NewOrderSingle nos);
    Task CancelOrderAsync(OrderCancelRequest ocr);
    Task CreateStrategyAsync(CreateStrategyRequest request);
    Task StartStrategyAsync(int strategyId);
}

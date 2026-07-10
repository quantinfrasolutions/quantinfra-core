using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Common.Interfaces.Api.Management;
using QuantInfra.Common.Interfaces.Api.Strategies;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Accounts;
using QuantInfra.Sdk.Trading.Orders;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Services.ManagementCore;

public class ManagementServiceClient(ManagementService service) : IManagementServiceClient
{
    public Task<int> CreateAccountAsync(CreateAccountRequest request) => service.CreateAccountAsync(request, 0);

    public Task CreateSubaccountAsync(CreateSubaccountRequest request) => service.CreateSubaccountAsync(request, 0);

    public Task<IReadOnlyDictionary<int, decimal>> GetBalancesAsync(int accountId) =>
        service.GetBalancesAsync(accountId);

    public Task<int> CreateBalanceOperationAsync(NewBalanceOperation request) => service.CreateBalanceOperationAsync(request, 0);

    public Task<IReadOnlyCollection<Position>> GetActivePositionsAsync(int accountId) =>
        service.GetPositionsAsync(accountId);

    public Task<IReadOnlyCollection<OrderStatus>> GetActiveOrdersAsync(int accountId) =>
        service.GetOrdersAsync(accountId);

    public Task PlaceOrderAsync(NewOrderSingle nos) => service.PlaceOrderAsync(nos, 0);

    public Task CancelOrderAsync(OrderCancelRequest ocr) => service.CancelOrderAsync(ocr, 0);

    public Task CreateStrategyAsync(CreateStrategyRequest request) => service.CreateStrategyAsync(request, 0);

    public Task StartStrategyAsync(int strategyId) => service.StartStrategyAsync(strategyId, 0);

    public Task CreateTradingClientConfig(TradingClientConfig request) =>
        service.CreateTradingClientConfigAsync(request, 0);

    public Task DeleteTradingClientConfig(int accountId) =>
        service.DeleteTradingClientConfigAsync(accountId, 0);
}
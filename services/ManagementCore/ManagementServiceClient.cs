using Common.Accounts.Abstractions;
using Common.Trading.Positions;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Common.Interfaces.Api.Management;
using QuantInfra.Common.Interfaces.Api.Strategies;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Services.ManagementCore;

public class ManagementServiceClient(ManagementService service) : IManagementServiceClient
{
    public Task CreateAccountAsync(CreateAccountRequest request) => service.CreateAccountAsync(request, 0);

    public Task CreateSubaccountAsync(CreateSubaccountRequest request) => service.CreateSubaccountAsync(request, 0);

    public Task<IReadOnlyDictionary<int, decimal>> GetBalancesAsync(int accountId) =>
        service.GetBalancesAsync(accountId);

    public Task CreateBalanceOperationAsync(NewBalanceOperation request) => service.CreateBalanceOperationAsync(request, 0);

    public Task<IReadOnlyCollection<Position>> GetActivePositionsAsync(int accountId) =>
        service.GetPositionsAsync(accountId);

    public Task<IReadOnlyCollection<OrderStatus>> GetActiveOrdersAsync(int accountId) =>
        service.GetOrdersAsync(accountId);

    public Task PlaceOrderAsync(NewOrderSingle nos) => service.PlaceOrderAsync(nos, 0);

    public Task CancelOrderAsync(OrderCancelRequest ocr) => service.CancelOrderAsync(ocr, 0);

    public Task CreateStrategyAsync(CreateStrategyRequest request) => service.CreateStrategyAsync(request, 0);

    public Task StartStrategyAsync(int strategyId) => service.StartStrategyAsync(strategyId, 0);
}
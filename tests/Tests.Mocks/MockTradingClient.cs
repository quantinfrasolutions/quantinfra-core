// using Common.Accounts.Abstractions.ExternalAccounts;
// using Common.Messaging;
// using Common.Trading.ExternalAccounts;
// using Common.Trading.Infrastructure;
// using Microsoft.Extensions.Logging;
// using NodaTime;
// using QuantInfra.Common.Accounts.Abstractions;
// using QuantInfra.Common.Accounts.Abstractions.ExternalAccounts;
// using QuantInfra.Common.Messaging;
// using QuantInfra.Sdk.Accounts.Abstractions;
// using QuantInfra.Sdk.Accounts.Abstractions.ExternalAccounts;
// using QuantInfra.Sdk.Trading.ExternalAccounts;
//
// namespace QuantInfra.Tests.Mocks;
//
// public class MockTradingClient(
//     TradingClientConfig config, 
//     IServiceProvider serviceProvider, 
//     ILoggerFactory loggerFactory,
//     IPublisher publisher,
//     IClock clock
// ) : IHostedTradingClient
// {
//     public void PlaceOrder(NewOrderSingleExternal order)
//     {
//         throw new NotImplementedException();
//     }
//
//     public void CancelOrder(OrderCancelRequestExternal ocr)
//     {
//         throw new NotImplementedException();
//     }
//
//     public void ReplaceOrder(OrderReplaceRequestExternal ocr)
//     {
//         throw new NotImplementedException();
//     }
//
//     public bool StartAsyncCalled { get; private set; }
//     public Task StartAsync(CancellationToken cancellationToken)
//     {
//         StartAsyncCalled = true;
//         return Task.CompletedTask;
//     }
//
//     public Task StopAsync(CancellationToken cancellationToken)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<ExternalAccountTradesReport> GetAccountTradesAsync()
//     {
//         throw new NotImplementedException();
//     }
//
//     public bool IsConnected()
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<AccountOrdersSnapshot> GetAccountOrdersSnapshotAsync()
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<AccountPositionsSnapshot> GetAccountPositionsSnapshotAsync()
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<ExternalAccountFullSnapshot> GetAccountFullSnapshotAsync()
//     {
//         throw new NotImplementedException();
//     }
//
//     public void RequestAccountOrdersSnapshot(Guid? requestId = null)
//     {
//         throw new NotImplementedException();
//     }
//
//     public void RequestAccountFullSnapshot(IReadOnlyDictionary<string, Instant> lastReceivedTradeDts,
//         Instant? lastReceivedBalanceOperationDt, Guid? requestId = null)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task<ExternalAccountFullSnapshot> GetAccountFullSnapshotAsync(bool sendUpdateToBus = false)
//     {
//         throw new NotImplementedException();
//     }
//
//     public bool RequestAccountFullSnapshotCalled { get; private set; } 
//     public void RequestAccountFullSnapshot(Guid cmdRequestId)
//     {
//         RequestAccountFullSnapshotCalled = true;
//     }
// }
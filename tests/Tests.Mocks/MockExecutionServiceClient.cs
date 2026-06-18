// using QuantInfra.Sdk.Accounts.Abstractions.ExternalAccounts;
//
// namespace QuantInfra.Tests.Mocks;
//
// public class MockExecutionServiceClient : IExecutionServiceClient
// {
//     public Task SubscribeToExternalAccountExecutions(int accountId, string esName, bool waitForInitialSnapshot,
//         int timeoutMilliseconds)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
//
//     public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
//
//     public void OnExternalAccountSnapshot(ExternalAccountFullSnapshot snapshot)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Task SubscribeToRequestsTopic(string accountServiceName)
//     {
//         throw new NotImplementedException();
//     }
// }
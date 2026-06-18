// using Common.Trading.Infrastructure;
// using QuantInfra.Common.Accounts.Abstractions;
//
// namespace QuantInfra.Tests.Mocks;
//
// public class MockTradingAccountsRepositoryReadonly : ITradingAccountsRepositoryReadonly
// {
//     public List<AccountRecordV6> Accounts { get; set; } = new();
//
//     public Task<IReadOnlyCollection<AccountRecordV6>> GetTradingAccountsByExecutionServiceId(
//         string executionServiceName) =>
//         Task.FromResult((IReadOnlyCollection<AccountRecordV6>)Accounts);
// }
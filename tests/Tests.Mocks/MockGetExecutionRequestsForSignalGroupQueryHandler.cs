// using Common.EventSourcing;
// using Domain.Queries.Accounts;
// using QuantInfra.Common.Accounts.Abstractions.ExecutableAccounts;
//
// namespace QuantInfra.Tests.Mocks;
//
// public class MockGetExecutionRequestsForSignalGroupQueryHandler : IQueryHandler<GetProcessedExecutionRequestsForSignalGroup,
//     IReadOnlyCollection<ExecutionRequestStatusReport>>
// {
//     public List<ExecutionRequestStatusReport> ExecutionRequests { get; } = new();
//
//     public IReadOnlyCollection<ExecutionRequestStatusReport> Handle(GetProcessedExecutionRequestsForSignalGroup query) =>
//         ExecutionRequests;
//
//     public Task<IReadOnlyCollection<ExecutionRequestStatusReport>> HandleAsync(GetProcessedExecutionRequestsForSignalGroup query)
//     {
//         throw new NotImplementedException();
//     }
// }
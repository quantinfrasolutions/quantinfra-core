// using Common.Accounts.Abstractions;
// using Common.EventSourcing;
// using Domain.Queries.Accounts;
// using Microsoft.Extensions.Logging;
// using QuantInfra.Common.Accounts.Abstractions;
// using QuantInfra.Common.Strategies.DefaultExecutionPolicies;
//
// namespace QuantInfra.Tests.Mocks;
//
// public class MockExecutionPoliciesProvider : IExecutionPoliciesProvider, IQueryHandler<GetExecutionPolicy, IExecutionPolicy>
// {
//     private ExecutionPolicyDefinition _epDefinition;
//     private MarketExecutionPolicy _policy;
//     
//     public MockExecutionPoliciesProvider(ILoggerFactory loggerFactory)
//     {
//         _epDefinition = new(Guid.NewGuid(), "MarketExecutionPolicy", new Dictionary<string, string>());
//         _policy = new MarketExecutionPolicy(_epDefinition, loggerFactory);
//     }
//
//
//     public IExecutionPolicy GetExecutionPolicy(Guid epId) => _policy;
//     public IExecutionPolicy Handle(GetExecutionPolicy query) => GetExecutionPolicy(query.ExecutionPolicyId);
//
//     public async Task<IExecutionPolicy> HandleAsync(GetExecutionPolicy query) => GetExecutionPolicy(query.ExecutionPolicyId);
//
//     public Guid GetExecutionPolicyId() => _policy.ExecutionPolicyId;
// }
// using Common.EventSourcing;
// using Domain.Queries.StaticData;
// using QuantInfra.Common.StaticData.Abstractions;
//
// namespace QuantInfra.Tests.Mocks;
//
// public class MockContractQueriesHandler : 
//     IQueryHandler<GetContract, Contract>,
//     IQueryHandler<GetContractByExternalId, Contract?>
// {
//     public Dictionary<long, Contract> Contracts { get; } = new();
//     
//     public Contract Handle(GetContract query) => Contracts[query.ContractId];
//
//     public Task<Contract> HandleAsync(GetContract query)
//     {
//         throw new NotImplementedException();
//     }
//
//     public Contract? Handle(GetContractByExternalId query) =>
//         Contracts.Values.SingleOrDefault(c => !string.IsNullOrEmpty(c.ExternalContractId) && c.ExternalContractId == query.ExternalId);
//
//     public Task<Contract?> HandleAsync(GetContractByExternalId query)
//     {
//         throw new NotImplementedException();
//     }
// }
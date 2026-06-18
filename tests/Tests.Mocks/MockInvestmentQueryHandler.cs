// using Common.EventSourcing;
// using Domain.Queries.Accounts;
//
// namespace QuantInfra.Tests.Mocks;
//
// public class MockInvestmentQueryHandler : IQueryHandler<GetAccountInvestment, decimal>
// {
//     public Dictionary<Guid, decimal> Investments { get; } = new();
//
//     public decimal Handle(GetAccountInvestment query) => Investments[query.AccountId];
//
//     public async Task<decimal> HandleAsync(GetAccountInvestment query) => Handle(query);
// }
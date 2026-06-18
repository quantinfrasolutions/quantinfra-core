// using Common.EventSourcing;
// using Common.Trading;
// using Domain.Queries.Accounts;
//
// namespace QuantInfra.Tests.Mocks;
//
// public class MockGetTradeByExternalIdQueryHandler : IQueryHandler<GetTradeByExternalId, Trade?>
// {
//     public List<Trade> Trades { get; } = new();
//
//     public Trade? Handle(GetTradeByExternalId query) =>
//         Trades.SingleOrDefault(t => t.ExternalTradeId == query.ExternalTradeId && t.AccountId == query.AccountId);
//
//     public Task<Trade?> HandleAsync(GetTradeByExternalId query)
//     {
//         throw new NotImplementedException();
//     }
// }
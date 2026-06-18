// using Common.EventSourcing;
// using Common.Trading.Orders;
// using Domain.Queries.Accounts;
//
// namespace QuantInfra.Tests.Mocks;
//
// public class MockGetOrdersHistoryHandler : IQueryHandler<GetOrdersHistory, IReadOnlyCollection<ExecutionReport>>
// {
//     public List<ExecutionReport> History { get; } = new();
//     public IReadOnlyCollection<ExecutionReport> Handle(GetOrdersHistory query) => 
//         History.Where(er => er.OrderId == query.Filter.OrderId).ToList();
//
//     public Task<IReadOnlyCollection<ExecutionReport>> HandleAsync(GetOrdersHistory query)
//     {
//         throw new NotImplementedException();
//     }
// }
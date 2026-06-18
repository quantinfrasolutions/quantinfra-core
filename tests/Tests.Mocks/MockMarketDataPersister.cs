// using Common.MarketData;
// using NodaTime;
//
// namespace QuantInfra.Tests.Mocks;
//
// public class MockMarketDataPersister : IMarketDataPersister
// {
//     public void AppendBAU(ExchangeBar bau, BarAggregationType aggType)
//     {
//     }
//
//     public Task AppendBAUAsync(ExchangeBar bar, BarAggregationType aggType) => Task.CompletedTask;
//
//     public Instant GetLastPersistedOpenDt(int streamId) => Instant.MinValue;
// }
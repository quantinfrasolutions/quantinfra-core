namespace Common.StaticData.Abstractions;
using Stream = QuantInfra.Sdk.StaticData.Stream;

public interface IMarketDataServiceStreamsRepository
{
    Task<IReadOnlyCollection<Stream>> GetEnabledStreamsAsync(string? serviceName);
}
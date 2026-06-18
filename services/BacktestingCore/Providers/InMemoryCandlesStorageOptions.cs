using NodaTime;

namespace BacktestingCore.Providers
{
    public struct InMemoryCandlesStorageOptions
    {
        public InMemoryCandlesStorageOptions(
            Instant startDt,
            Instant endDt,
            Duration storageTimeframe, 
            bool useCache
        )
        {
            StartDt = startDt;
            EndDt = endDt;
            StorageTimeframe = storageTimeframe;
            UseCache = useCache;
        }

        public Instant StartDt { get; init; }
        public Instant EndDt { get; init; }
        public Duration StorageTimeframe { get; init; }
        public bool UseCache { get; init; }
    }
}
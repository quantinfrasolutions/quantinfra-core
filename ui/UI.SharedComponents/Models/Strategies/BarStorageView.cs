using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.StaticData;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace UI.SharedComponents.Models.Strategies
{
    public class BarStorageView : BarStorageConfig
	{
		public Stream? Stream { get; set; }
        public Contract? Contract { get; set; }        

        public BarStorageConfig ToBarStorageConfig() => new BarStorageConfig
        {
            Id = Id,
            IdType = IdType,
            AggregationType = AggregationType,
            Timeframe = Timeframe,
            Offset = Offset ?? NodaTime.Period.Zero,
            Timezone = Timezone ?? "UTC"
        };

        public BarStorageView() { }

        public BarStorageView(
            BarStorageConfig c,
            Dictionary<long, Stream> streams,
            Dictionary<long, Contract> contracts
        ) : base(c)
        {
            Stream = c.IdType == IdType.Stream ? streams[c.Id] : null;
            Contract = c.IdType == IdType.Contract ? contracts[c.Id] : null;
        }
	}
}


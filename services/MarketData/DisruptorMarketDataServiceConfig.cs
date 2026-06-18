namespace QuantInfra.Services.MarketData
{
    public class DisruptorMarketDataServiceConfig : Config
    {
        public string CheckpointDatabasePath { get; set; } = "checkpoint.db";
        public int CheckpointDatabaseCommitInverval { get; set; } = 1024;
    }
}

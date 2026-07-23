using NodaTime;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Connectors.Ibkr.Interfaces;

public class HistoricalDataDownloadRequest
{
    public bool UseRTH { get; set; }
    public int ConId { get; set; }
    public string Exchange { get; set; }
    public int Days { get; set; }
    public Instant? To { get; set; }
    public SubscriptionType Type { get; set; }
    /// <summary>
    /// https://ibkrcampus.com/campus/ibkr-api-page/twsapi-doc/#hist-bar-size
    /// </summary>
    public string BarSize { get; set; } = "1 min";

    public override string ToString() =>
        $"{{ HistoricalDataDownloadRequest | UseRTH: {UseRTH}, ConId: {ConId}, Exchange: {Exchange}, Days: {Days}, To: {To}, Type: {Type}, BarSize={BarSize} }}";
}
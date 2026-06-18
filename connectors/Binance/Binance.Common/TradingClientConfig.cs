using QuantInfra.Connectors.Common;

namespace QuantInfra.Connectors.Binance.Common;

public class TradingClientConfig : BaseConfig
{
    public string RestUri { get; set; }
    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }
    public int RenewListenerKeyPeriodMinutes { get; set; } = 50;
}
using QuantInfra.Connectors.Common;

namespace QuantInfra.Connectors.Binance.Common;

public class TradingClientConfig : BaseConfig
{
    public string RestUri { get; set; }
    public string ApiKey { get; set; }
    /// <summary>
    /// Binance USD-M WebSocket API endpoint used for trading commands. When omitted, the production or testnet
    /// endpoint is inferred from <see cref="RestUri"/>.
    /// </summary>
    public string? WebSocketApiUri { get; set; }
    public int RenewListenerKeyPeriodMinutes { get; set; } = 50;
}

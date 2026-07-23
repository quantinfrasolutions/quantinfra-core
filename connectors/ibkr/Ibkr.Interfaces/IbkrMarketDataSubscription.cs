using QuantInfra.Ibkr.Interfaces;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Connectors.Ibkr.Interfaces;

public class IbkrMarketDataSubscription : IMarketDataSubscription
{
    public IbkrMarketDataSubscription() { }

    public IbkrMarketDataSubscription(IbkrMarketDataSubscriptionRequest request, int requestId)
    {
        SubscriptionId = requestId;
        ConId = request.ConId;
        Ticker = request.Ticker;
        SecurityType = request.SecurityType;
        Currency = request.Currency;
        Exchange = request.Exchange;
        FuturesLastDateOrContractMonth = request.FuturesLastDateOrContractMonth ?? "";
        LocalSymbol = request.LocalSymbol;
        SubscriptionType = request.SubscriptionType;
        UseRTH = request.UseRTH;
        StreamId = request.StreamId;
    }

    public IbkrMarketDataSubscription(IbkrMarketDataSubscription s)
    {
        SubscriptionId = s.SubscriptionId;
        ConId = s.ConId;
        Ticker = s.Ticker;
        SecurityType = s.SecurityType;
        Currency = s.Currency;
        Exchange = s.Exchange;
        FuturesLastDateOrContractMonth = s.FuturesLastDateOrContractMonth;
        LocalSymbol = s.LocalSymbol;
        SubscriptionType = s.SubscriptionType;
        UseRTH = s.UseRTH;
        StreamId = s.StreamId;
        ClientName = s.ClientName;
        LastBar = s.LastBar;
    }

    public int SubscriptionId { get; init; }
    public int ConId { get; init; }
    public string Ticker { get; init; }
    public SecType SecurityType { get; init; }
    public string Currency { get; init; }
    public string Exchange { get; init; }        
    public string FuturesLastDateOrContractMonth { get; init; }
    public string LocalSymbol { get; init; }        
    public SubscriptionType SubscriptionType { get; init; }
    public bool UseRTH { get; init; }
    public int? StreamId { get; init; }
    public string ClientName { get; set; }
    public ExchangeBar? LastBar { get; set; }

    public override string ToString()
    {
        return $"{nameof(SubscriptionId)}: {SubscriptionId}, {nameof(ConId)}: {ConId}, {nameof(Ticker)}: {Ticker}, {nameof(SecurityType)}: {SecurityType}, {nameof(Currency)}: {Currency}, {nameof(Exchange)}: {Exchange}, {nameof(FuturesLastDateOrContractMonth)}: {FuturesLastDateOrContractMonth}, {nameof(LocalSymbol)}: {LocalSymbol}, {nameof(SubscriptionType)}: {SubscriptionType}, {nameof(UseRTH)}: {UseRTH}, {nameof(StreamId)}: {StreamId}, {nameof(ClientName)}: {ClientName}, {nameof(LastBar)}: {LastBar}";
    }
}
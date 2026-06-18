using System.Globalization;

namespace QuantInfra.Common.Messaging.Patterns.TopicMulticast;

public static class TopicDefinitions
{
    public static string HeartbeatsTopic = "h";
    public static string AccountUpdatesTopicPrefix = "a";
    public static string StrategyUpdatesTopicPrefix = "s";
    public static string OrderBookUpdatesTopicPrefix = "ob";
    
    public static string GetAccountUpdatesTopic(int accountId) => $"{AccountUpdatesTopicPrefix}.{accountId.ToString(CultureInfo.InvariantCulture)}";
    public static string GetExecutionsTopic(int accountId) => $"e.{accountId.ToString(CultureInfo.InvariantCulture)}";
    public static string GetStrategyUpdatesTopic(int strategyId) => $"{StrategyUpdatesTopicPrefix}.{strategyId.ToString(CultureInfo.InvariantCulture)}";
    public static string GetCandles1mTopic(long streamId) => $"st.c1m.{streamId}";
    public static string GetPriceUpdatesTopic() => $"c.price";
    public static string GetPriceUpdatesTopic(long contractId) => $"c.price.{contractId}";
    public static string GetAccountServiceRequestsTopic(string accountServiceName) => $"req.{accountServiceName}";

    public static string GetFullOrderBookTopic(int contractId) => $"c.ob.{contractId}";
    public static string GetBBOTopic(int contractId) => $"c.bbo.{contractId}";
}
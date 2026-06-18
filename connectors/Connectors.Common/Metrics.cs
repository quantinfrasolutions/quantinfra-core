using Prometheus;

namespace QuantInfra.Connectors.Common;

public class TradingConnectorMetrics
{
    private static readonly Lazy<Histogram> _orderHop = new(() => Prometheus.Metrics.CreateHistogram(
        "new_order_exchange_time",
        "Time difference between sending an order to the exchange and receiving the first response, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(50000, 50000, 20) }
    ));
    public static Histogram ReceiveBarHop => _orderHop.Value;
}
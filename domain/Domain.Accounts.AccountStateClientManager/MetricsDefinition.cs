using System;
using Prometheus;

namespace QuantInfra.Domain.Accounts.AccountStateClientManager;

internal static class MetricsDefinition
{
    private static readonly Lazy<Histogram> _brokerOrderRoundtrip = new(() => Metrics.CreateHistogram(
        "broker_order_roundtrip",
        "Time difference sending an order and receiving an initial response from the broker, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(100000, 100000, 10) }
    ));
    public static Histogram BrokerOrderRoundrtip => _brokerOrderRoundtrip.Value;
    
    private static readonly Lazy<Histogram> _accountsServiceRoundtrip = new(() => Metrics.CreateHistogram(
        "accounts_service_order_roundtrip",
        "Time difference sending an order and receiving an initial response from AS, us",
        new HistogramConfiguration() { Buckets = Histogram.LinearBuckets(10000, 10000, 20) }
    ));
    public static Histogram AccountsServiceRoundrtip => _accountsServiceRoundtrip.Value;
}
// using AccountsCore;
// using Microsoft.Extensions.Diagnostics.HealthChecks;
// using NodaTime;
//
// namespace QuantInfra.Core.Apps.MonolithTradingService.HealthChecks
// {
// #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
//     internal class MarketDataProcessedConfig
//     {
//         public Duration UnhealthyDuration { get; set; } = Duration.FromSeconds(60);
//     }
//
//
//     internal class MarketDataProcessed(MarketDataProcessedConfig config, AccountServiceState state) : IHealthCheck
// 	{
//         // ReSharper disable once MethodSupportsCancellation
//
//         public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) => Task.Run(() =>
//         {
//             var lastProcessedHeartbeatTs = state.LastMarketDataEvtProcessingTs ?? Instant.MinValue;
//             var diff = SystemClock.Instance.GetCurrentInstant() - lastProcessedHeartbeatTs; 
//             if (diff > config.UnhealthyDuration)
//                 return HealthCheckResult.Unhealthy($"Last market data event processed {(int)diff.TotalSeconds} ago");
//             
//             return HealthCheckResult.Healthy();
//         });
//     }
// #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
// }
//

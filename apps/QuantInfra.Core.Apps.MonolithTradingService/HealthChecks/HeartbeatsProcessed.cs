// using AccountsCore;
// using Microsoft.Extensions.Diagnostics.HealthChecks;
// using NodaTime;
//
// namespace QuantInfra.Core.Apps.MonolithTradingService.HealthChecks
// {
// #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
//     internal class HeartbeatsProcessedConfig
//     {
//         public Duration UnhealthyDuration { get; set; } = Duration.FromSeconds(30);
//     }
//
//
//     internal class HeartbeatsProcessed(HeartbeatsProcessedConfig config, AccountServiceState state) : IHealthCheck
// 	{
//         // ReSharper disable once MethodSupportsCancellation
//
//         public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) => Task.Run(() =>
//         {
//             var lastProcessedHeartbeatTs = state.LastProcessedHeartbeatTs ?? Instant.MinValue;
//             var diff = SystemClock.Instance.GetCurrentInstant() - lastProcessedHeartbeatTs; 
//             if (diff > config.UnhealthyDuration)
//                 return HealthCheckResult.Unhealthy($"Last heartbeat processed {(int)diff.TotalSeconds} ago");
//             
//             return HealthCheckResult.Healthy();
//         });
//     }
// #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
// }
//

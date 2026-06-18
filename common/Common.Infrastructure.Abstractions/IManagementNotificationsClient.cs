using Microsoft.Extensions.Hosting;
using NodaTime;

namespace QuantInfra.Common.Infrastructure.Abstractions;

public interface IManagementNotificationsClient : IHostedService
{
    void PublishMessage(object message, Instant dt);
}
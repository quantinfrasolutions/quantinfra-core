using Common.Infrastructure.Abstractions;
using NodaTime;
using QuantInfra.Common.Infrastructure.Abstractions;

namespace QuantInfra.Tests.Mocks;

public class MockManagementNotificationsClient : IManagementNotificationsClient
{
    public List<object> Messages { get; } = new();
    
    public void PublishMessage(object message, Instant dt)
    {
        Messages.Add(message);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
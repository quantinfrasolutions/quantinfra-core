using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Services.MonolithService;

public class HostedComponent : IComponentExceptionHandler
{
    private readonly ILogger _logger;
    private readonly Func<HostedComponent, IServiceProvider> _buildServiceProvider;
    private readonly Func<IServiceProvider, IEnumerable<IHostedService>> _getStartupServices;
    private IServiceProvider? _serviceProvider;
    
    public HostedComponent(
        string name,
        ILogger logger,
        Func<HostedComponent, IServiceProvider> buildServiceProvider, 
        Func<IServiceProvider, IEnumerable<IHostedService>> getStartupServices
    )
    {
        Name = name;
        _logger = logger;
        _buildServiceProvider = buildServiceProvider;
        _getStartupServices = getStartupServices;
    }
    
    public string Name { get; }
    public ComponentStatus Status { get; private set; } = ComponentStatus.Stopped;
    public Exception? Exception { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Exception = null;
        try
        {
            _logger.LogInformation($"Starting component {Name}");
            _serviceProvider = _buildServiceProvider(this);
            var services = _getStartupServices(_serviceProvider).ToList();
            await Task.WhenAll(services.Select(s => s.StartAsync(CancellationToken.None)));
            Status = ComponentStatus.Running;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error starting component {Name}", Name);
            Exception = e;
            Status = ComponentStatus.Failed;
            try
            {
                await StopAsync(cancellationToken);
            }
            catch (Exception stopEx)
            {
                _logger.LogError(stopEx, "Error starting component {Name}", Name);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Status != ComponentStatus.Running) return;
        _logger.LogInformation("Stopping component {Name}", Name);
        try
        {
            await Task.WhenAll(_getStartupServices(_serviceProvider).Select(s => s.StopAsync(CancellationToken.None)));
            _logger.LogInformation("Stopped component {Name}", Name);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error stopping component {Name}", Name);
        }
        finally
        {
            _serviceProvider = null!;
        }
    }

    public void Raise(Exception exception)
    {
        Status = ComponentStatus.Failed;
        Exception = exception;
        Task.Run(async () => await StopAsync(CancellationToken.None));
    }
}

public enum ComponentStatus
{
    Running,
    Failed,
    Stopped
}
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace QuantInfra.Services.MonolithService;

public class HostedComponent
{
    private readonly ILogger _logger;
    private readonly Func<IServiceProvider> _buildServiceProvider;
    private readonly Func<IServiceProvider, IEnumerable<IHostedService>> _getStartupServices;
    private IServiceProvider _serviceProvider;
    
    public HostedComponent(
        string name,
        ILogger logger,
        Func<IServiceProvider> buildServiceProvider, 
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

    public async Task StartAsync()
    {
        List<IHostedService> services = new();
        try
        {
            _logger.LogInformation($"Starting component {Name}");
            _serviceProvider = _buildServiceProvider();
            services = _getStartupServices(_serviceProvider).ToList();
            await Task.WhenAll(services.Select(s => s.StartAsync(CancellationToken.None)));
            Status = ComponentStatus.Running;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error starting component {Name}", Name);
            Status = ComponentStatus.Failed;
            try
            {
                await Task.WhenAll(services.Select(s => s.StopAsync(CancellationToken.None)));
            }
            catch (Exception shutdownException)
            {
                _logger.LogError(shutdownException, "Error stopping component {Name}", Name);
            }
            finally
            {
                _serviceProvider = null!;
            }
        }
    }

    public async Task StopAsync()
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

    public async Task RestartAsync()
    {
        await StopAsync();
        await StartAsync();
    }
}

public enum ComponentStatus
{
    Running,
    Failed,
    Stopped
}
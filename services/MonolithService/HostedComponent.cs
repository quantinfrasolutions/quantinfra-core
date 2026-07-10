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

    private Task? _startupExceptionTask = null;
    private ManualResetEventSlim? _startupExceptionEvent = null;
    private Exception? _startupException = null;
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Exception = null;
        var ct = new CancellationTokenSource();
        
        try
        {
            _logger.LogInformation($"Starting component {Name}");
            _serviceProvider = _buildServiceProvider(this);
            var services = _getStartupServices(_serviceProvider).ToList();
            
            _startupExceptionEvent = new();
            _startupExceptionTask = Task.Run(() => _startupExceptionEvent.Wait(ct.Token), cancellationToken);
            var startupTask = Task.WhenAll(services.Select(s => s.StartAsync(CancellationToken.None)));

            if (await Task.WhenAny(startupTask, _startupExceptionTask) == _startupExceptionTask)
            {
                throw _startupException!;
            }

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
        finally
        {
            await ct.CancelAsync();
            _startupExceptionTask = null;
            _startupException = null;
            _startupExceptionEvent = null;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (Status != ComponentStatus.Running) return Task.CompletedTask;
        return StopAsync(cancellationToken);
    }

    private async Task StopAsyncInternal(CancellationToken cancellationToken)
    {
        if (_serviceProvider is null) return;
        
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
        if (_startupExceptionTask is {IsCompleted: false})
        {
            _startupException = exception;
            _startupExceptionEvent!.Set();
            return;
        }
        Task.Run(async () => await StopAsyncInternal(CancellationToken.None));
    }
}

public enum ComponentStatus
{
    Running,
    Failed,
    Stopped
}
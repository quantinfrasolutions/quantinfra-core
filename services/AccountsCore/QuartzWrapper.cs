using System.Threading;
using System.Threading.Tasks;
using QuantInfra.Common.Messaging;
using Quartz;

namespace QuantInfra.Services.AccountsCore;

public class QuartzWrapper(QuartzHostedService service) : IIncomingTransport
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return service.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return service.StopAsync(cancellationToken);
    }
}
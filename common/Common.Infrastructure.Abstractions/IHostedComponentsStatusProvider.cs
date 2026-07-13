using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuantInfra.Common.Infrastructure.Abstractions;

public interface IHostedComponentsStatusProvider
{
    Task<IReadOnlyCollection<HostedComponentStatus>> GetHostedComponentsAsync();
    Task StartComponent(string component);
    Task StopComponent(string component);
}
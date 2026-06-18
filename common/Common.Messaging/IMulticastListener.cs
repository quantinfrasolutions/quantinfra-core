using Microsoft.Extensions.Hosting;

namespace QuantInfra.Common.Messaging;

public interface IMulticastListener : IHostedService
{
    void Subscribe(string? topicPrefix = null, bool controlSequence = false, string? sequenceResetTargetCompId = null);
}
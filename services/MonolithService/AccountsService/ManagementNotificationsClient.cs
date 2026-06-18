using Common.Metrics;
using Disruptor.Dsl;
using Domain.Queries.Accounts.AccountsService;
using NodaTime;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.InProcess;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Domain.Commands.Accounts.AccountsService;
using QuantInfra.Domain.Commands.StaticData;
using QuantInfra.Domain.Events.Accounts.Management;
using QuantInfra.Domain.Events.Strategies.Management;
using TransportMessage = QuantInfra.Common.Messaging.InProcess.TransportMessage;

namespace QuantInfra.Services.MonolithService.AccountsService;

public class ManagementNotificationsClient(AccountsCore.Config config, Topology topology, Disruptor<IncomingDisruptorMessage> disruptor, IClock clock) : 
    Listener(disruptor, clock), 
    IManagementNotificationsClient,
    IIncomingTransport
{
    private readonly string _serviceName = config.AccountServiceName;
    private readonly long _sessionId = clock.GetCurrentInstant().ToUnixTimeMilliseconds();
    private long _seqNo = 0;
    
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
        topology.SubscribeToTopic("management", this);
    }

    public void PublishMessage(object message, Instant dt)
    {
        var swNow = MetricsUtils.GetUnixMicro();
        disruptor.PublishMessage(new TransportMessage("management", MessageType.DataMessage, _sessionId,
            _seqNo++, MetricsUtils.GetUnixMicro(), message), clock.GetCurrentInstant().ToUnixTimeMilliseconds(), swNow);
    }

    protected override bool CheckMessage(ITransportMessage message, string? topicName)
    {
        return message is TransportMessage msg && (
            (msg.Data is AccountCreatedEvt acEvt && acEvt.Account.AccountServiceName == _serviceName)
            || (msg.Data is IAccountsServiceCmd cmd && cmd.AccountServiceName == _serviceName)
            || (msg.Data is IAccountServiceAsyncQuery q && q.AccountServiceName == _serviceName)
            || (msg.Data is StrategyCreatedEvt scEvt && scEvt.Account.AccountServiceName == _serviceName)
            || (msg.Data is SubaccountAssignedEvt sa && sa.AccountServiceName == _serviceName)
            || (msg.Data is ClearStaticDataCacheCmd csd && csd.AccountServiceName == _serviceName)
        );
    }
}
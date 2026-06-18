using Disruptor.Dsl;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Services.AccountsCore;
using TransportMessage = QuantInfra.Common.Messaging.InProcess.TransportMessage;

namespace QuantInfra.Services.MonolithService.AccountsService;

public class OutputToInputDisruptorPublisher(Disruptor<IncomingDisruptorMessage> disruptor) : IOutputToInputDisruptorPublisher
{
    public void PublishMessage(string senderCompId, object o)
    {
        var msg = new TransportMessage(senderCompId, MessageType.DataMessage, 0, 0, 0, o);
        disruptor.PublishMessage(msg, 0, 0);
    }
}
using Disruptor.Dsl;
using QuantInfra.Common.Messaging;

namespace QuantInfra.Common.ServiceBase
{
    public static class Extensions
    {
        public static void PublishMessage(this Disruptor<IncomingDisruptorMessage> disruptor, ITransportMessage message,
            long receivedAt, long swReceivedAt, bool isReplay = false, long? walPartition = null)
        {
            using var scope = disruptor.PublishEvent();
            var data = scope.Event();
            data.ReceiveMessage(message, receivedAt, swReceivedAt, isReplay);
            if (walPartition.HasValue) data.WalPartition = walPartition.Value;
        }
        
        public static void PublishMessage(this Disruptor<IncomingDisruptorMessage> disruptor, object message, long walPartition, bool isReplay = false)
        {
            using var scope = disruptor.PublishEvent();
            var data = scope.Event();
            data.SetParsedMessage(message);
            data.SetIsReplay(isReplay);
            data.WalPartition = walPartition;
        }

        public static void PublishParsedMessage(this Disruptor<IncomingDisruptorMessage> disruptor, object message, long swReceivedAt)
        {
            using var scope = disruptor.PublishEvent();
            var data = scope.Event();
            data.SetParsedMessage(message);
            data.SetSwReceivedAt(swReceivedAt);
        }
        
        public static void PublishMessage<TMessage>(this Disruptor<OutgoingDisruptorMessage> disruptor, TMessage message, 
            long swPublishedAt = 0, long swReceivedAt = 0)
        {
            using var scope = disruptor.PublishEvent();
            var data = scope.Event();
            data.Value = message;
            data.SwReceivedAt = swReceivedAt;
            data.SwPublishedAt = swPublishedAt;
        }
    }
}
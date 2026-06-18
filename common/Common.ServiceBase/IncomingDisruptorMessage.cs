using QuantInfra.Common.Messaging;

namespace QuantInfra.Common.ServiceBase
{
    public class IncomingDisruptorMessage
    {
        public ITransportMessage? TransportMessage { get; private set; } = null!;
        public bool IsReplay { get; private set; }
        // Unix milliseconds timestamp as of receiving the message
        public long ReceivedAt { get; private set; }
        // Stopwatch value in microseconds as of receiving the message. Use to calculate internal processing time. 
        public long SwReceivedAt { get; private set; }
        public object? ParsedMessage { get; private set; }
        // Set this flag to true if further processing is not required for the message
        public bool Skip { get; set; }
        public long WalPartition { get; set; }

        public void ReceiveMessage(ITransportMessage message, long receivedAt, long swReceivedAt, bool isReplay)
        {
            TransportMessage = message;
            ReceivedAt = receivedAt;
            SwReceivedAt = swReceivedAt;
            IsReplay = isReplay;
        }
        
        public void SetParsedMessage(object? parsedMessage)
        {
            ParsedMessage = parsedMessage;
        }

        public void SetIsReplay(bool isReplay) => IsReplay = isReplay;
        
        public void SetSwReceivedAt(long swReceivedAt) => SwReceivedAt = swReceivedAt;
    }
}
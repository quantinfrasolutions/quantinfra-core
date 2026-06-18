namespace QuantInfra.Common.ServiceBase
{
    public class OutgoingDisruptorMessage
    {
        public object Value { get; set; }
        public string? Payload { get; set; }
        public long TransportSequence { get; set; }
        public bool Skip { get; set; }
        /// <summary>
        /// Stopwatch microseconds value as of receiving the original message by the component
        /// </summary>
        public long SwReceivedAt { get; set; }
        /// <summary>
        /// Stopwatch microseconds value as of placing the message into the output disruptor
        /// </summary>
        public long SwPublishedAt { get; set; }
    }
}
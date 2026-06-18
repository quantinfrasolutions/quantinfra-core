namespace QuantInfra.Common.Messaging.Sockets;

public class MessageHeader
{
    public MessageHeader(string senderCompId, long msgSeqNum)
    {
        SenderCompId = senderCompId;
        MsgSeqNum = msgSeqNum;
    }

    public string SenderCompId { get; }
    public long MsgSeqNum { get; }
}
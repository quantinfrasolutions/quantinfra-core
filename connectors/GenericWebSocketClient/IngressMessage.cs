namespace GenericWebSocketClient;

public readonly struct IngressMessage
{
    public readonly byte[] Buffer;
    public readonly int Length;
    public readonly long ReceivedAt;
    public readonly long SwReceivedAt;

    public IngressMessage(byte[] buffer, int length, long receivedAt, long swReceivedAt)
    {
        Buffer = buffer;
        Length = length;
        ReceivedAt = receivedAt;
        SwReceivedAt = swReceivedAt;
    }

    public ReadOnlySpan<byte> AsSpan() => new(Buffer, 0, Length);
}
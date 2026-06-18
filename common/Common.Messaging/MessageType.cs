namespace QuantInfra.Common.Messaging;

public enum MessageType
{
    SessionStart = 0,
    FillGap = 1,
    SequenceReset = 2,
    DataMessage = 3,
}
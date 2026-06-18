namespace QuantInfra.Common.ServiceBase.ServiceMessages;

public class PersistStateEvt
{
    public PersistStateEvt(string serializedState, long partitionId)
    {
        SerializedState = serializedState;
        PartitionId = partitionId;
    }

    public string SerializedState { get; }
    public long PartitionId { get; }
}
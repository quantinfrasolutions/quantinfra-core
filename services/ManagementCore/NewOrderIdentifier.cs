namespace ManagementCore;

public struct NewOrderIdentifier
{
    public NewOrderIdentifier(int accountId, string clOrdId)
    {
        AccountId = accountId;
        ClOrdId = clOrdId;
    }

    public int AccountId { get; init; }
    public string ClOrdId { get; init; }
}
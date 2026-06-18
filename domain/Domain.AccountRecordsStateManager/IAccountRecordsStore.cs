using System.Collections.Generic;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Domain.AccountRecordsStateManager;

public interface IAccountRecordsStore
{
    Dictionary<int, AccountRecordV6> AccountRecords { get; set; }
    /// <summary>
    /// AccountId.Classifier => SubaccountId
    /// </summary>
    Dictionary<int, Dictionary<SubaccountType, List<Subaccount>>> Subaccounts { get; set; }
    /// <summary>
    /// SubaccountId.Classifier => AccountId
    /// </summary>
    Dictionary<int, Dictionary<SubaccountType, List<int>>> ReverseSubaccounts { get; set; }
}

class DefaultStore : IAccountRecordsStore
{
    public Dictionary<int, AccountRecordV6> AccountRecords { get; set; } = new();
    public Dictionary<int, Dictionary<SubaccountType, List<Subaccount>>> Subaccounts { get; set; } = new();
    public Dictionary<int, Dictionary<SubaccountType, List<int>>> ReverseSubaccounts { get; set; } = new();
}
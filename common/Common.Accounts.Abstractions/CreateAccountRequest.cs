using System.ComponentModel.DataAnnotations;
using QuantInfra.Sdk.Accounts;

namespace QuantInfra.Common.Accounts.Abstractions;

public class CreateAccountRequest
{
    public CreateAccountRequest() { }
    
    public CreateAccountRequest(
        string accountServiceName,
        string? name,
        AccountType accountType = AccountType.VirtualAccount,
        int currencyId = 840,
        PositionAccounting positionAccounting = PositionAccounting.Netted,
        bool enableSharePriceTracking = true,
        bool includeUnrealizedPnLToMtm = true,
        bool? addInitialInvestment = null,
        int? brokerId = null
    )
    {
        AccountServiceName = accountServiceName;
        Name = name;
        AccountType = accountType;
        CurrencyId = currencyId;
        PositionAccounting = positionAccounting;
        EnableSharePriceTracking = enableSharePriceTracking;
        IncludeUnrealizedPnLToMtm = includeUnrealizedPnLToMtm;
        BrokerId = brokerId;
        AddInitialInvestment = addInitialInvestment ?? accountType == AccountType.VirtualAccount;
    }

    public string? Name { get; set; }
    public AccountType AccountType { get; set; }
    public int CurrencyId { get; set; } = 840;
    public PositionAccounting PositionAccounting { get; set; } = PositionAccounting.Netted;
    public bool EnableSharePriceTracking { get; set; }
    public bool IncludeUnrealizedPnLToMtm { get; set; }
    public int? BrokerId { get; set; }
    
    [Required(ErrorMessage = "Account service is required")] 
    public string AccountServiceName { get; set; }
    
    public bool AddInitialInvestment { get; set; }
}
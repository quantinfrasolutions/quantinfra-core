using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class ContractTemplateListView
{
    public ContractTemplateListView(int templateId, string name, SecurityType securityType,
        PnLCalculatorType plCalculatorType, int? assetId, string? assetName, decimal minSize, decimal? minSizeMoney,
        decimal maxSize, decimal? maxSizeMoney, decimal sizeIncrement, decimal tickSize, decimal? tickValue,
        decimal priceQuotation, int settlementCurrencyId, string settlementCurrencyName, int? baseCurrencyId,
        string? baseCurrencyName, int? quoteCurrencyId, string? quoteCurrencyName, int? defaultDatafeedId,
        string? defaultDatafeedName, int exchangeId, string exchangeName, int brokerId, string brokerName,
        int daysInYear, string? description)
    {
        TemplateId = templateId;
        Name = name;
        SecurityType = securityType;
        PlCalculatorType = plCalculatorType;
        AssetId = assetId;
        AssetName = assetName;
        MinSize = minSize;
        MinSizeMoney = minSizeMoney;
        MaxSize = maxSize;
        MaxSizeMoney = maxSizeMoney;
        SizeIncrement = sizeIncrement;
        TickSize = tickSize;
        TickValue = tickValue;
        PriceQuotation = priceQuotation;
        SettlementCurrencyId = settlementCurrencyId;
        SettlementCurrencyName = settlementCurrencyName;
        BaseCurrencyId = baseCurrencyId;
        BaseCurrencyName = baseCurrencyName;
        QuoteCurrencyId = quoteCurrencyId;
        QuoteCurrencyName = quoteCurrencyName;
        DefaultDatafeedId = defaultDatafeedId;
        DefaultDatafeedName = defaultDatafeedName;
        ExchangeId = exchangeId;
        ExchangeName = exchangeName;
        BrokerId = brokerId;
        BrokerName = brokerName;
        DaysInYear = daysInYear;
        Description = description;
    }

    public int TemplateId { get; init; }
    public string Name { get; init; }
    public SecurityType SecurityType { get; init; }
    public PnLCalculatorType PlCalculatorType { get; init; }
    public int? AssetId { get; init; }
    public string? AssetName { get; init; }
    public decimal MinSize { get; init; }
    public decimal? MinSizeMoney { get; init; }
    public decimal MaxSize { get; init; }
    public decimal? MaxSizeMoney { get; init; }
    public decimal SizeIncrement { get; init; }
    public decimal TickSize { get; init; }
    public decimal? TickValue { get; init; }
    public decimal PriceQuotation { get; init; }
    public int SettlementCurrencyId { get; init; }
    public string SettlementCurrencyName { get; init; }
    public int? BaseCurrencyId { get; init; }
    public string? BaseCurrencyName { get; init; }
    public int? QuoteCurrencyId { get; init; }
    public string? QuoteCurrencyName { get; init; }   
    public int? DefaultDatafeedId { get; init; }
    public string? DefaultDatafeedName { get; init; }   
    public int ExchangeId { get; init; }
    public string ExchangeName { get; init; }   
    public int BrokerId { get; init; }
    public string BrokerName { get; init; }  
    public int DaysInYear { get; init; } = 252;
    public string? Description { get; init; }
}
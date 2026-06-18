using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class CreateContractTemplateRequest
{
    public int TemplateId { get; set; }
    public string Name { get; set; }
    public SecurityType SecurityType { get; set; }
    public PLCalculatorType PlCalculatorType { get; set; } = PLCalculatorType.Default;
    public int? AssetId { get; set; }
    public decimal MinSize { get; set; } = 1;
    public decimal? MinSizeMoney { get; set; }
    public decimal MaxSize { get; set; } = 1000000m;
    public decimal? MaxSizeMoney { get; set; }
    public decimal SizeIncrement { get; set; } = 1;
    public decimal TickSize { get; set; } = 0.01m;
    public decimal? TickValue { get; set; }
    public decimal PriceQuotation { get; set; } = 1;
    public int SettlementCurrencyId { get; set; }
    public int? BaseCurrencyId { get; set; }
    public int? QuoteCurrencyId { get; set; }
    public int? DefaultDatafeedId { get; set; }
    public List<int> CommissionIds { get; set; }
    public List<int> TradingSessionsIds { get; set; }
    public int ExchangeId { get; set; } // TODO: contract may be listed on several exchanges, with its own commission structures and trading sessions
    public int BrokerId { get; set; } // TODO: contract may be provided by several brokers, with its own commission structures
    public int DaysInYear { get; set; } = 252;
    public string? Description { get; set; }
}
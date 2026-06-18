using NodaTime;
using QuantInfra.Sdk.StaticData.Synthetics;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class CreateContractRequest
{
    public string Ticker { get; set; }
        
    public int? TemplateId { get; set; }
        
    public LocalDate? FirstTradingDate { get; init; }
    public LocalDate? ExpirationDate { get; init; }
        
    public SyntheticContractType? SyntheticContractType { get; set; }
    public bool? SynthRequiresBarRecalculationAtRollover { get; set; }
    
    public string? ExternalContractId { get; set; }
    public int? AssetId { get; set; }
    public string? Description { get; set; }
    
    public int DefaultDatafeedId { get; set; }
}
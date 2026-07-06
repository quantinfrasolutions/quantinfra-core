using System.ComponentModel.DataAnnotations;
using NodaTime;
using QuantInfra.Sdk.StaticData.Synthetics;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class CreateContractRequest
{
    [Required(ErrorMessage = "Ticker is required")] public string Ticker { get; set; }
        
    [Required(ErrorMessage = "Contract template is required")] public int TemplateId { get; set; }
        
    public string? FirstTradingDate { get; set; }
    public string? ExpirationDate { get; set; }
        
    public SyntheticContractType? SyntheticContractType { get; set; }
    public bool? SynthRequiresBarRecalculationAtRollover { get; set; }
    
    public string? ExternalContractId { get; set; }
    public int? AssetId { get; set; }
    public string? Description { get; set; }
    
    public int DefaultDatafeedId { get; set; }
}
using NodaTime;
using QuantInfra.Sdk.StaticData.Synthetics;

namespace QuantInfra.Common.Interfaces.Api.StaticData;

public class ContractListView
{
    public ContractListView(int contractId, string ticker, ContractTemplateListView template, LocalDate? firstTradingDate, LocalDate? expirationDate, SyntheticContractType? syntheticContractType, bool? synthRequiresBarRecalculationAtRollover, string? externalContractId, int? assetId, string? assetName, string? description)
    {
        ContractId = contractId;
        Ticker = ticker;
        Template = template;
        FirstTradingDate = firstTradingDate;
        ExpirationDate = expirationDate;
        SyntheticContractType = syntheticContractType;
        SynthRequiresBarRecalculationAtRollover = synthRequiresBarRecalculationAtRollover;
        ExternalContractId = externalContractId;
        AssetId = assetId;
        AssetName = assetName;
        Description = description;
    }

    public int ContractId { get; init; }
    public string Ticker { get; init; }
    public ContractTemplateListView Template { get; init; }
    public LocalDate? FirstTradingDate { get; init; }
    public LocalDate? ExpirationDate { get; init; }
    public SyntheticContractType? SyntheticContractType { get; init; }
    public bool? SynthRequiresBarRecalculationAtRollover { get; init; }
    public string? ExternalContractId { get; init; }
    public int? AssetId { get; init; }
    public string? AssetName { get; init; }
    public string? Description { get; init; }
}
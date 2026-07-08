using NodaTime;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Orders;

namespace QuantInfra.Databases.Main.Models.Events;

internal readonly struct ExternalExecutionReportEvtData(BrokerType brokerType, string? externalContractId)
{
    public BrokerType BrokerType { get; } = brokerType;
    public string? ExternalContractId { get; } = externalContractId;
}

internal readonly struct NewUnmappedContractRegisteredEvtData(string externalContractId)
{
    public string ExternalContractId { get; } = externalContractId;
}

internal readonly struct StrategyLastCalculationTsEvtData(Instant ts)
{
    public Instant Ts { get; } = ts;
}

internal readonly struct AccountReconciliationStatusChangedEvtData(bool needsReconciliation, string message)
{
    public bool NeedsReconciliation { get; } = needsReconciliation;
    public string Message { get; } = message;
}

internal readonly struct OrderCancelReplaceRejectEvtData(CxlRejReason reason, string? rejectText)
{
    public CxlRejReason Reason { get; } = reason;
    public string? RejectText { get; } = rejectText;
}

internal readonly struct TradeEvtData(int assetId, PnLCalculatorOptions options)
{
    public int AssetId { get; } = assetId;
    public PnLCalculatorOptions Options { get; } = options;
}

internal readonly struct AccountEndOfDayEvtData(IReadOnlyDictionary<int, PnLCalculatorOptions> options)
{
    public IReadOnlyDictionary<int, PnLCalculatorOptions> Options { get; } = options;
}
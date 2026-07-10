namespace QuantInfra.Common.Interfaces.Api.Accounts;

public record BrokerAccountReconciliationStatus(
    bool IsReconciled,
    IReadOnlyCollection<string> ReconciliationMessages,
    IReadOnlyCollection<string> UnknownContracts,
    IReadOnlyCollection<string> UnknownAssets
);
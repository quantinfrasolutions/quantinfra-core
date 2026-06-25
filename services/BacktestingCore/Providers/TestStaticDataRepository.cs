using System;
using System.Collections.Generic;
using System.Linq;
using Common.StaticData.Abstractions;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Services.BacktestingCore.Providers;

public class TestStaticDataRepository : IStaticDataProvider
{
    private readonly Dictionary<int, Currency> _currencies = new();
    public Currency? GetCurrency(int id) => _currencies.GetValueOrDefault(id);

    private readonly Dictionary<int, Contract> _contracts = new();
    private readonly Dictionary<int, Dictionary<string, Contract>> _contractsByExternalId = new();
    
    public Contract? GetContract(int contractId) => _contracts.GetValueOrDefault(contractId);

    public Contract? GetContractByExternalId(int brokerId, string externalContractId) =>
        _contractsByExternalId.GetValueOrDefault(brokerId)?.GetValueOrDefault(externalContractId);

    private readonly HashSet<int> _fxConversionContracts = new();
    public IReadOnlyCollection<int> GetFxConversionContractIds() => _fxConversionContracts;

    private readonly Dictionary<int, Dictionary<string, Asset>> _assetsByExternalId = new();
    public Asset? GetAssetByExternalId(int brokerId, string externalAssetId) =>
        _assetsByExternalId.GetValueOrDefault(brokerId)?.GetValueOrDefault(externalAssetId);

    public IReadOnlyCollection<Contract> GetContracts(IEnumerable<int> contractIds) =>
        contractIds.Select(id => _contracts.GetValueOrDefault(id)).Where(c => c != null).ToList()!;

    public (int contractId, bool isDirect) GetFxConversionContract(int fromCcyId, int toCcyId)
    {
        throw new System.NotImplementedException();
    }

    private readonly Dictionary<int, Broker> _brokers = new();
    public Broker? GetBroker(int brokerId) => _brokers.GetValueOrDefault(brokerId);

    public string? GetContractOrderBookSubscriptionServiceName(int contractId) =>
        throw new NotSupportedException();
}
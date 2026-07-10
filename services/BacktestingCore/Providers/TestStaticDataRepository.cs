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
    private readonly Dictionary<int, Dictionary<int, Tuple<int, bool>>> _fxConversions = new();
    public IReadOnlyCollection<int> GetFxConversionContractIds() => _fxConversionContracts;
    
    private readonly Dictionary<int, Asset> _assets = new();
    public Asset? GetAsset(int assetId) => _assets.GetValueOrDefault(assetId);

    private readonly Dictionary<int, Dictionary<string, Asset>> _assetsByExternalId = new();
    public Asset? GetAssetByExternalId(int brokerId, string externalAssetId) =>
        _assetsByExternalId.GetValueOrDefault(brokerId)?.GetValueOrDefault(externalAssetId);

    public IReadOnlyCollection<Contract> GetContracts(IEnumerable<int> contractIds) =>
        contractIds.Select(id => _contracts.GetValueOrDefault(id)).Where(c => c != null).ToList()!;

    public (int contractId, bool isDirect) GetFxConversionContract(int fromCcyId, int toCcyId)
    {
        var tup = _fxConversions[fromCcyId][toCcyId];
        return (tup.Item1, tup.Item2);
    }

    private readonly Dictionary<int, Broker> _brokers = new();
    public Broker? GetBroker(int brokerId) => _brokers.GetValueOrDefault(brokerId);

    public string? GetContractOrderBookSubscriptionServiceName(int contractId) =>
        throw new NotSupportedException();

    private readonly Dictionary<int, Stream> _streams = new();
    private Dictionary<int, int?> _streamsToContracts = new();
    public IReadOnlyCollection<Stream> GetStreams(IEnumerable<int> streamIds) =>
        streamIds.Select(id => _streams.GetValueOrDefault(id)).Where(s => s != null).ToList()!;

    private readonly Dictionary<int, ConstantStreamValue> _constantStreams = new();
    public IReadOnlyDictionary<int, ConstantStreamValue> ConstantStreams => _constantStreams;

    public Contract? GetContractForConstantStream(int csStreamId)
    {
        if (_streamsToContracts.TryGetValue(csStreamId, out var contractId)
            && contractId is not null && _contracts.TryGetValue(contractId.Value, out var contract)
        ) return contract;
        return null;
    }

    public void TryAddContract(Contract contract)
    {
        if (!_contracts.TryAdd(contract.ContractId, contract)) return;
        if (contract.DefaultStream is not null) TryAddStream(contract.DefaultStream, contract.ContractId);
        if (contract.Template.BaseCurrency is not null) TryAddCurrency(contract.Template.BaseCurrency);
        if (contract.Template.QuoteCurrency is not null) TryAddCurrency(contract.Template.QuoteCurrency);
        TryAddCurrency(contract.Template.SettlementCurrency);
        // foreach (var ts in contract.Template.TradingSessions) TryAddTradingSession(ts);
    }

    public void TryAddFxConversionContract(Contract contract)
    {
        TryAddContract(contract);
        _fxConversionContracts.Add(contract.ContractId);

        var baseCcyId = contract.Template.BaseCurrency!.CurrencyId;
        var settlCcyId = contract.Template.SettlementCurrency!.CurrencyId;
        _fxConversions.TryAdd(baseCcyId, new());
        _fxConversions[baseCcyId].TryAdd(settlCcyId, new(contract.ContractId, true));
        _fxConversions.TryAdd(settlCcyId, new());
        _fxConversions[settlCcyId].TryAdd(baseCcyId, new (contract.ContractId, false));
    }

    public void TryAddStream(Stream stream, int? contractId)
    {
        if (_streams.TryAdd(stream.StreamId, stream))
            _streamsToContracts.Add(stream.StreamId, contractId);
    }

    public void TryAddConstantStreamValue(ConstantStreamValue csv)
    {
        _constantStreams.TryAdd(csv.StreamId, csv);
    }

    public void TryAddCurrency(Currency currency)
    {
        _assets.TryAdd(currency.CurrencyId, currency.Asset);
        _currencies.TryAdd(currency.CurrencyId, currency);
    }
}
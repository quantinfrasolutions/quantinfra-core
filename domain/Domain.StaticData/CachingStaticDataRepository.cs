using Common.StaticData.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Commands.StaticData;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.StaticData;

// TODO: add external event handlers for invalidating cache
public class CachingStaticDataRepository(IStaticDataProvider backendRepository, IStaticDataRepositoryStateStore stateStore) :
    IQueryHandler<GetContract, Contract?>,
    IQueryHandler<GetContracts, IReadOnlyCollection<Contract>>,
    IQueryHandler<GetCurrency, Currency?>,
    IQueryHandler<GetConversionPath, IReadOnlyCollection<FxConversionStep>>,
    IQueryHandler<GetContractByExternalId, Contract?>,
    IQueryHandler<GetAssetByExternalId, Asset?>,
    IQueryHandler<GetContractOrderBookSubscriptionServiceName, string?>,
    IQueryHandler<GetBroker, Broker?>,
    ICommandHandler<ClearStaticDataCacheCmd>
{
    public Broker Handle(GetBroker query)
    {
        if (!stateStore.Brokers.TryGetValue(query.BrokerId, out var broker))
        {
            broker = backendRepository.GetBroker(query.BrokerId);
            stateStore.Brokers[query.BrokerId] = broker;
        }

        return broker;
    }
    
    public Contract? Handle(GetContract query)
    {
        if (!stateStore.Contracts.TryGetValue(query.ContractId, out var contract))
        {
            contract = backendRepository.GetContract(query.ContractId);
            TryInstantiateContract(query.ContractId, contract);
        }

        return contract;
    }
    
    public IReadOnlyCollection<Contract> Handle(GetContracts query) => GetContracts(query.ContractIds);
    
    public Contract? Handle(GetContractByExternalId query)
    {
        if (!stateStore.ContractsByExternalId.TryGetValue(query.BrokerId, out var contracts)
            || !contracts.TryGetValue(query.ExternalId, out var contract))
        {
            contract = backendRepository.GetContractByExternalId(query.BrokerId, query.ExternalId);
            if (contract is not null)
                TryInstantiateContract(contract.ContractId, contract);
            else
            {
                stateStore.ContractsByExternalId.TryAdd(query.BrokerId, new());
                stateStore.ContractsByExternalId[query.BrokerId][query.ExternalId] = null;
            }
        }
        
        return contract;
    }
    
    public Asset? Handle(GetAssetByExternalId query)
    {
        if (!stateStore.AssetsByExternalId.TryGetValue(query.BrokerId, out var assets)
            || !assets.TryGetValue(query.ExternalId, out var asset))
        {
            asset = backendRepository.GetAssetByExternalId(query.BrokerId, query.ExternalId);
            stateStore.ContractsByExternalId.TryAdd(query.BrokerId, new());
            stateStore.ContractsByExternalId[query.BrokerId][query.ExternalId] = null;
        }
        
        return asset;
    }
    
    public Currency? Handle(GetCurrency query)
    {
        if (!stateStore.Currencies.TryGetValue(query.CurrencyId, out var currency))
        {
            currency = backendRepository.GetCurrency(query.CurrencyId);
            stateStore.Currencies[query.CurrencyId] = currency;
        }
        
        return stateStore.Currencies[query.CurrencyId];
    }
    
    public IReadOnlyCollection<FxConversionStep> Handle(GetConversionPath query)
    {
        if (stateStore.ConversionPaths.TryGetValue(query.FromCurrencyId, out var p1)
            && p1.TryGetValue(query.ToCurrencyId, out var path)
           ) return path;

        // Load all conversions
        var conversionContractIds = backendRepository.GetFxConversionContractIds();
        var conversionContracts = GetContracts(conversionContractIds);
        foreach (var cc in conversionContracts)
        {
            if (cc.Template.BaseCurrency is null || cc.Template.QuoteCurrency is null) continue;
            
            stateStore.ConversionPaths.TryAdd(cc.Template.BaseCurrency.CurrencyId, new());
            stateStore.ConversionPaths[cc.Template.BaseCurrency.CurrencyId].TryAdd(
                cc.Template.QuoteCurrency.CurrencyId, 
                new List<FxConversionStep> { new() { ContractId = cc.ContractId, IsDirect = true } }
            );
            
            stateStore.ConversionPaths.TryAdd(cc.Template.QuoteCurrency.CurrencyId, new());
            stateStore.ConversionPaths[cc.Template.QuoteCurrency.CurrencyId].TryAdd(
                cc.Template.BaseCurrency.CurrencyId, 
                new List<FxConversionStep> { new() { ContractId = cc.ContractId, IsDirect = false } }
            );
        }
        // Fallback for the requested path
        stateStore.ConversionPaths.TryAdd(query.FromCurrencyId, new());
        stateStore.ConversionPaths[query.FromCurrencyId].TryAdd(query.ToCurrencyId, new List<FxConversionStep>());
        stateStore.ConversionPaths.TryAdd(query.ToCurrencyId, new());
        stateStore.ConversionPaths[query.ToCurrencyId].TryAdd(query.FromCurrencyId, new List<FxConversionStep>());
        
        return stateStore.ConversionPaths[query.FromCurrencyId][query.ToCurrencyId];
    }

    public string? Handle(GetContractOrderBookSubscriptionServiceName query) =>
        backendRepository.GetContractOrderBookSubscriptionServiceName(query.ContractId); // TODO: add caching. Not critical, since this happens only upon SS startup

    private IReadOnlyCollection<Contract> GetContracts(IReadOnlyCollection<int> contractIds)
    {
        var missingContracts = contractIds.Except(stateStore.Contracts.Keys).ToList();
        if (missingContracts.Count > 0)
        {
            var contracts = backendRepository.GetContracts(missingContracts).ToDictionary(c => c.ContractId);
            foreach (var cid in missingContracts)
            {
                TryInstantiateContract(cid, contracts.GetValueOrDefault(cid));
            }
        }
        return contractIds.Select(cid => stateStore.Contracts[cid]).Where(c => c is not null).ToList()!;
    }

    private void TryInstantiateContract(int contractId, Contract? contract)
    {
        stateStore.Contracts[contractId] = contract;

        if (contract is not null)
        {
            stateStore.Currencies.TryAdd(contract.Template.SettlementCurrency.CurrencyId, contract.Template.SettlementCurrency);
            if (contract.Template.BaseCurrency is not null)
            {
                stateStore.Currencies.TryAdd(contract.Template.BaseCurrency.CurrencyId, contract.Template.BaseCurrency);
            }

            if (contract.Template.QuoteCurrency is not null)
            {
                stateStore.Currencies.TryAdd(contract.Template.QuoteCurrency.CurrencyId, contract.Template.QuoteCurrency);
            }

            if (!string.IsNullOrEmpty(contract.ExternalContractId))
            {
                stateStore.ContractsByExternalId.TryAdd(contract.Template.Broker.BrokerId, new());
                stateStore.ContractsByExternalId[contract.Template.Broker.BrokerId][contract.ExternalContractId] = contract;
            }
        }
    }

    public void Handle(ClearStaticDataCacheCmd cmd)
    {
        stateStore.Contracts.Clear();
        stateStore.ContractsByExternalId.Clear();
        stateStore.Currencies.Clear();
        stateStore.ConversionPaths.Clear();
        stateStore.Brokers.Clear();
        stateStore.AssetsByExternalId.Clear();
    }
}
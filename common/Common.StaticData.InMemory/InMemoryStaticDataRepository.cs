using System.Runtime.CompilerServices;
using Common.StaticData.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.StaticData.Synthetics;
using QuantInfra.Sdk.Trading;
using Contract = QuantInfra.Sdk.StaticData.Contract;
using Stream = QuantInfra.Sdk.StaticData.Stream;

[assembly: InternalsVisibleTo("Tests.Unit.MDS")]
[assembly: InternalsVisibleTo("QuantInfra.Databases.Main.Repositories")]
[assembly: InternalsVisibleTo("BacktestingCore")]

namespace QuantInfra.Common.StaticData.InMemory;

public class InMemoryStaticDataRepository : 
    // IStaticDataManagementRepository,
    IStaticDataProvider,
    IMarketDataServiceStreamsRepository
{
    protected internal Dictionary<int, Exchange?> Exchanges { get; } = new();
    protected internal Dictionary<int, TradingSession?> TradingSessions { get; } = new();
    protected internal Dictionary<int, CommissionStructure?> Commissions { get; } = new();
    protected internal Dictionary<int, Currency?> Currencies { get; } = new();
    protected internal Dictionary<int, Asset?> Assets { get; } = new();
    protected internal Dictionary<string, Asset?> AssetsByExternalId { get; } = new();
    protected internal Dictionary<int, ContractTemplate?> ContractTemplates { get; } = new();
    protected internal Dictionary<int, Contract?> Contracts { get; } = new();
    protected internal Dictionary<int, Dictionary<string, Contract?>> ContractsByExternalId { get; } = new();
    // protected internal Dictionary<int, BaseTradeSize?> BaseTradeSizes { get; } = new();
    protected internal Dictionary<int, Stream?> Streams { get; } = new();
    protected internal Dictionary<int, ConstantStreamValue> ConstantStreams { get; } = new();
    protected internal Dictionary<int, Stream?> StreamsByContract { get; } = new();
    protected internal Dictionary<int, Datafeed?> Datafeeds { get; } = new();
    protected internal Dictionary<int, Broker?> Brokers { get; } = new();
    protected internal List<Contract> FxConvesionContracts { get; } = new();
    protected internal Dictionary<int, Dictionary<int, (int, bool)>> DirectConversions { get; } = new();
    protected internal Dictionary<int, Dictionary<int, (int, bool)>> ReverseConversions { get; } = new();
    protected internal Dictionary<int, List<TradingSession>> TradingSessionsByExchange { get; } = new();
    protected internal Dictionary<int, Dictionary<string, Currency?>> CurrenciesByExternalName { get; } = new();
    
    public IEnumerable<Exchange> GetExchanges() => Exchanges.Values.Where(e => e != null)!;
    
    public void CreateExchange(Exchange exchange)
    {
        // exchange.ExchangeId = GetNextId(exchange.ExchangeId, Exchanges, 100);
        Exchanges.Add(exchange.ExchangeId, exchange);
    }

    public async Task<IEnumerable<Exchange>> GetExchangesAsync() => GetExchanges();
    
    public async Task CreateExchangeAsync(Exchange exchange) => CreateExchange(exchange);

    public IEnumerable<TradingSession> GetTradingSessions() => TradingSessions.Values;

    public IEnumerable<TradingSession> GetTradingSessions(int exchangeId) => TradingSessions
        .Values
        .Where(ts => ts.ExchangeId == exchangeId);

    public async Task<IEnumerable<TradingSession>> GetTradingSessionsAsync() => GetTradingSessions();

    public async Task<IEnumerable<TradingSession>> GetTradingSessionsAsync(int exchangeId) =>
        GetTradingSessions(exchangeId);

    public virtual void CreateTradingSession(TradingSession ts)
    {
        // throw new NotImplementedException();
        // ts.TradingSessionId = GetNextId(ts.TradingSessionId, TradingSessions, 1000);
        TradingSessions.Add(ts.TradingSessionId, ts);
    }

    public virtual async Task CreateTradingSessionAsync(TradingSession ts) =>
        CreateTradingSession(ts);

    public void CreateAsset(Asset asset)
    {
        if (asset.AssetId == 0)
        {
            asset = new Asset()
            {
                AssetId = GetNextId(asset.AssetId, Assets, 1000),
                Name = asset.Name,
                AssetType = asset.AssetType,
                Description = asset.Description,
            };
        }
        Assets.Add(asset.AssetId, asset);
    }

    public void CreateCurrency(Currency currency)
    {
        Currencies.Add(currency.CurrencyId, currency);
    }

    public void CreateCommission(CommissionStructure commissionStructure)
    {
        throw new NotImplementedException();
        // commissionStructure.CommissionId = GetNextId(commissionStructure.CommissionId, Commissions, 100);
        // Commissions.Add(commissionStructure.CommissionId, commissionStructure);
    }

    // public IEnumerable<ContractDefinition> GetContracts() => throw new NotImplementedException(); 
        // Contracts.Values;
    
    public IReadOnlyCollection<Contract> GetContracts(IEnumerable<int> contractIds)
    {
        var hs = contractIds.ToHashSet();
        return Contracts.Where(kv => hs.Contains(kv.Key)).Select(kv => kv.Value).ToList();
    }

    public Task LoadStreamsAsync(List<int> streamIds) => Task.CompletedTask;
    public void LoadContracts(ICollection<int> usedContractIds)
    {
    }

    // public ContractDefinition GetContractDefinition(int id) => throw new NotImplementedException(); 
        // Contracts[id];

    public virtual void CreateContract(Contract contract)
    {
        // throw new NotImplementedException();
        // if (contract.ContractId == default)
        // {
        //     contract = new Contract(contract, ContractTemplates, Assets, Currencies, Commissions, Exchanges, TradingSessions, Streams)
        //     {
        //         ContractId = GetNextId(contract.ContractId, Contracts, 10000)
        //     };
        // }
        Contracts.Add(contract.ContractId, contract);
    }

    public virtual void DeleteContract(int id)
    {
        Contracts.Remove(id);
    }

    public ContractTemplate GetContractTemplate(int templateId) => ContractTemplates[templateId];

    public void CreateContractTemplate(ContractTemplate template)
    {
        // throw new NotImplementedException();
        // if (template.TemplateId == default)
        // {
        //     template = new ContractTemplate(template)
        //     {
        //         TemplateId = GetNextId(template.TemplateId, Contracts, 10000)
        //     };
        // }
        ContractTemplates.Add(template.TemplateId, template);
    }

    public void UpdateContractTemplate(ContractTemplate template)
    {
        throw new NotImplementedException();
        // ContractTemplates[template.TemplateId] = template;
        // foreach (var c in Contracts.Values.ToArray())
        // {
        //     if (c.TemplateId == template.TemplateId)
        //     {
        //         DeleteContract(c.ContractId);
        //         CreateContract(c);
        //     }
        // }
    }

    public void DeleteContractTemplate(int templateId)
    {
        ContractTemplates.Remove(templateId);
    }
    

    public IEnumerable<CommissionStructure> GetCommissionStructures() => Commissions.Values;
    

    public Contract? GetContractByExternalId(int brokerId, string externalContractId) =>
        throw new NotImplementedException();
        // Contracts.Values
        //     .SingleOrDefault(c => c.ContractTemplate.BrokerId == brokerId && c.ExternalContractId == externalContractId);

    public string? GetContractExternalId(int contractId) => Contracts[contractId].ExternalContractId;

    public int? GetBrokerId(int contractId) =>
        throw new NotImplementedException();
        // Contracts[contractId].ContractTemplate.BrokerId;
        

    public int? GetContractIdByExternalContractId(string externalContractId, int brokerId) =>
        throw new NotImplementedException();
        // Contracts.Values.SingleOrDefault(c => c.ExternalContractId == externalContractId && c.ContractTemplate.BrokerId == brokerId)?.ContractId;

    // public void CreateSyntheticContractComposition(CreateSyntheticContractCompositionRequest request)
    // {
    //     throw new NotImplementedException();
    //     // var contract = Contracts[request.ContractId];
    //     //
    //     // if (!contract.IsSynthetic()) throw new InvalidOperationException($"Contract {request.ContractId} is not synthetic");
    //     // if (contract.SyntheticContractCompositionHistory is { Count: > 0 })
    //     //     throw new InvalidOperationException($"There is non-empty existing composition for contract {request.ContractId}");
    //     //
    //     // contract = new Contract(contract)
    //     // {
    //     //     SyntheticContractCompositionHistory = request.CompositionHistory
    //     //         .Select(ch => ch.ToComposition()).ToList()
    //     // };
    //     // Contracts[request.ContractId] = contract;
    // }
    //
    // public Task CreateSyntheticContractCompositionAsync(CreateSyntheticContractCompositionRequest request) =>
    //     Task.Run(() => CreateSyntheticContractComposition(request));

    // public void AddSyntheticContractCompositionHistory(int contractId, ContractCompositionModel composition)
    // {
    //     throw new NotImplementedException();
    //     // var contract = Contracts[contractId];
    //     //
    //     // var elements = contract.SyntheticContractCompositionHistory?.ToList()
    //     //                ?? new List<SyntheticContractComposition>();
    //     // elements.Add(composition.ToComposition());
    //     //
    //     // var newContract = new Contract(contract)
    //     // {
    //     //     SyntheticContractCompositionHistory = elements
    //     // };
    //     //
    //     // Contracts[contractId] = newContract;
    // }
    //
    // public Task AddSyntheticContractCompositionHistoryAsync(int contractId, ContractCompositionModel composition) =>
    //     Task.Run(() => AddSyntheticContractCompositionHistory(contractId, composition));
    
    public Task<ContractTemplate> GetContractTemplateAsync(int id)
    {
        throw new NotImplementedException();
    }
    
    
    public async Task<IEnumerable<CommissionStructure>> GetCommissionStructuresAsync() =>
        GetCommissionStructures();
    


    public IEnumerable<Currency> GetCurrencies() => Currencies.Values;

    public Contract GetContract(int contractId) => Contracts[contractId];

    // public IEnumerable<StreamDefinition> GetStreamDefinitions() => Streams.Values;

    public IEnumerable<Stream> GetStreams() => Streams.Values;

    // public IEnumerable<StreamDefinition> GetEnabledStreamDefinitions() =>
    //     GetEnabledStreams();

    // public StreamDefinition GetStreamDefinition(int id) => Streams[id];

    public Stream GetStream(int id) => Streams[id];

    public IReadOnlyCollection<Stream> GetEnabledStreams() => Streams.Values.ToList();

    // public virtual void CreateStream(StreamDefinition stream)
    // {
    //     stream.StreamId = stream.StreamId != 0 ? stream.StreamId : GetNextId(stream.StreamId, Streams, 10000);
    //     if (stream.ContractId.HasValue)
    //     {
    //         var contract = Contracts[stream.ContractId.Value];
    //         if (contract.ContractTemplate.DefaultDatafeedId == stream.DatafeedId)
    //         {
    //             contract.StreamId = stream.StreamId;
    //         }
    //     }
    //     Streams.Add(stream.StreamId, InstantiateStream(stream));
    // }

    // public virtual void UpdateStream(StreamDefinition stream)
    // {
    //     if (!Streams.ContainsKey(stream.StreamId))
    //     {
    //         throw new KeyNotFoundException($"Stream {stream.StreamId} doesn't exist");
    //     }
    //     Streams[stream.StreamId] = InstantiateStream(stream);
    // }

    // public async Task<IEnumerable<StreamDefinition>> GetStreamDefinitionsAsync() => GetStreamDefinitions();

    public async Task<IEnumerable<Stream>> GetStreamsAsync() => GetStreams();

    // public async Task<IEnumerable<StreamDefinition>> GetEnabledStreamDefinitionsAsync() =>
    //     GetEnabledStreamDefinitions();

    public async Task<IReadOnlyCollection<Stream>> GetEnabledStreamsAsync(string? serviceName) => GetEnabledStreams();

    public IReadOnlyCollection<int> GetFxConversionContractIds() => FxConvesionContracts.Select(c => c.ContractId).ToList();

    public Task<IReadOnlyCollection<int>> GetFxConversionContractsAsync() => Task.FromResult(GetFxConversionContractIds());

    public (int contractId, bool isDirect) GetFxConversionContract(int fromCcyId, int toCcyId) =>
        DirectConversions[fromCcyId][toCcyId];

    public int? GetStreamIdByContractId(int cid)
    {
        throw new NotImplementedException();
        // var dfIf = GetContract(cid).Template.DefaultDatafeedId;
        // return Streams.Values.SingleOrDefault(s => s.StreamId == cid && s.DatafeedId == dfIf)?.StreamId;
    }
    
    public SyntheticContractComposition InitializeSyntheticComposition(int contractId, Instant compValidFrom,
        double initialPrice, Dictionary<int, double> initialPrices)
    {
        throw new NotImplementedException();
        // var contract = GetContract(contractId);
        // var scs = contract.SyntheticContractCompositionHistory!.ToList();
        // var sc = scs.Single(s => s.ValidFrom == compValidFrom);
        // scs.Remove(sc);
        // var updatedSc = new SyntheticContractComposition(sc)
        // {
        //     InitialPrice = initialPrice,
        //     InitialPrices = initialPrices
        // };
        // scs.Add(updatedSc);
        // var newContract = new Contract(contract)
        // {
        //     SyntheticContractCompositionHistory = scs
        // };
        // Contracts[contractId] = newContract;
        // return updatedSc;
    }

    public Asset? GetAssetByExternalId(int brokerId, string externalAssetId) =>
        Assets.Values.SingleOrDefault(a => a.Name == externalAssetId);

    public Task<Asset?> GetAssetByExternalIdAsync(int brokerId, string externalAssetId) =>
        Task.Run(() => GetAssetByExternalId(brokerId, externalAssetId));

    // public async Task<StreamDefinition> GetStreamDefinitionAsync(int id) => GetStreamDefinition(id);

    public async Task<Stream> GetStreamAsync(int id) => GetStream(id);
    

    // public async virtual Task CreateStreamAsync(StreamDefinition stream) => CreateStream(stream);
    //
    // public async virtual Task UpdateStreamAsync(StreamDefinition stream) => UpdateStream(stream);

    // protected Contract InstantiateContract(ContractDefinition c) =>
    //     new Contract(c, ContractTemplates, Assets, Currencies, Commissions, Exchanges, TradingSessions, Streams);
    //
    // protected Stream InstantiateStream(StreamDefinition s) => new Stream(
    //     s, 
    //     s.ExchangeId.HasValue ? Exchanges[s.ExchangeId.Value].Timezone : null,
    //     TradingSessions
    // );

    public Currency GetCurrency(int id) => Currencies[id];
    public Exchange GetExchange(int id) => Exchanges[id];
    public Datafeed GetDatafeed(int id) => Datafeeds[id];
    public Asset GetAsset(int id) => Assets[id];
    public TradingSession GetTradingSession(int id) => TradingSessions[id];
    
    public IEnumerable<TradingSession> GetTradingSessionsForExchange(int exchangeId) =>
        TradingSessions.Values.Where(ts => ts.ExchangeId == exchangeId).ToList();

    public Broker GetBroker(int brokerId) => Brokers[brokerId];
    public string? GetContractOrderBookSubscriptionServiceName(int contractId)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyCollection<Stream> GetStreams(IEnumerable<int> streamIds)
    {
        throw new NotImplementedException();
    }

    public FxConversionStep GetFxConversionPath(int fromCcy, int toCcy)
    {
        throw new NotImplementedException();
    }

    public Currency GetCurrencyByExternalId(string externalName, int brokerId) =>
        Currencies.Values.Single(c => c.Asset.Name == externalName);

    // public void CreateDatafeed(Datafeed datafeed)
    // {
    //     datafeed.DatafeedId = GetNextId(datafeed.DatafeedId, Datafeeds, 100);
    //     Datafeeds.Add(datafeed.DatafeedId, datafeed);
    // }

    public IEnumerable<Broker> GetBrokers() => Brokers.Values.ToList();

    // public void CreateBroker(Broker broker)
    // {
    //     broker.BrokerId = GetNextId(broker.BrokerId, Datafeeds, 100);
    //     Brokers.Add(broker.BrokerId, broker);
    // }

    public async Task<IEnumerable<Broker>> GetBrokersAsync() => GetBrokers();

    // public async Task CreateBrokerAsync(Broker broker) => CreateBroker(broker);

    private int GetNextId<TValue>(int existingId, IDictionary<int, TValue> items, int initialId) =>
        existingId != 0 
            ? existingId 
            : items.Count > 0 
                ? items.Keys.Max() + 1 
                : initialId;

    public void CreateDatafeed(Datafeed datafeed)
    {
        Datafeeds.Add(datafeed.DatafeedId, datafeed);
    }

    public void CreateBroker(Broker broker)
    {
        Brokers.Add(broker.BrokerId, broker);
    }
}

public static class Extensions
{
    public static IServiceCollection UseInMemoryStaticDataRepository(this IServiceCollection sc) => sc
        .AddSingleton<InMemoryStaticDataRepository>()
        // .AddSingleton<IStaticDataManagementRepository>(sp => sp.GetService<InMemoryStaticDataRepository>()!)
        .AddSingleton<IStaticDataProvider>(sp => sp.GetService<InMemoryStaticDataRepository>()!)
        .AddSingleton<IMarketDataServiceStreamsRepository>(sp => sp.GetService<InMemoryStaticDataRepository>()!);
    
    public static IServiceCollection UseExternalInMemoryStaticDataRepository(this IServiceCollection sc, InMemoryStaticDataRepository repo) => sc
        .AddSingleton<InMemoryStaticDataRepository>(sp => repo)
        // .AddSingleton<IStaticDataManagementRepository>(sp => sp.GetService<InMemoryStaticDataRepository>()!)
        .AddSingleton<IStaticDataProvider>(sp => sp.GetService<InMemoryStaticDataRepository>()!)
        .AddSingleton<IMarketDataServiceStreamsRepository>(sp => sp.GetService<InMemoryStaticDataRepository>()!);
}
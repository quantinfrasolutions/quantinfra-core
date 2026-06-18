using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.MarketData;
using QuantInfra.Domain.Queries.MarketData;
using QuantInfra.Domain.Queries.StaticData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Domain.MarketData;

public class LastContractPricesStore:
    IEventHandler<ContractLastPriceUpdatedEvt>,
    IQueryHandler<GetLastKnownContractPrices, IReadOnlyDictionary<int, decimal>>,
    IQueryHandler<GetLastKnownContractPrice, decimal?>,
    IQueryHandler<GetConversionRate, decimal?>
{
    private readonly ILastContractPricesStore _store;
    private readonly IQueryBus _queryBus;

    public LastContractPricesStore(ILastContractPricesStore store, IQueryBus queryBus)
    {
        _store = store;
        _queryBus = queryBus;
    }

    public void Handle(ContractLastPriceUpdatedEvt evt)
    {
        _store.LastPrices[evt.ContractId] = new LastPrice(evt.Price, evt.ReferenceDt);
    }

    public IReadOnlyDictionary<int, decimal> Handle(GetLastKnownContractPrices query) =>
        _store.LastPrices.ToDictionary(kv => kv.Key, kv => kv.Value.Price);

    public decimal? Handle(GetLastKnownContractPrice query) => _store.LastPrices.GetValueOrDefault(query.ContractId)?.Price;
    
    public decimal? Handle(GetConversionRate query)
    {
        var conversionPath = _queryBus.Query<GetConversionPath, IReadOnlyCollection<FxConversionStep>>(new(query.FromCcy, query.ToCcy));
        if (conversionPath.Count == 0) return null;

        var rate = 1m;
        foreach (var c in conversionPath)
        {
            if (!_store.LastPrices.TryGetValue(c.ContractId, out var lp)) return null;
            var price = lp.Price;
            if (price == 0) return null;
            rate *= c.IsDirect ? price : 1 / price;
        }
        
        return rate;
    }
}

public static class LastContractPricesStorageConfigurationExtensions
{
    public static IServiceCollection AddLastContractPricesStorage(this IServiceCollection sc) => sc
        .AddSingleton<LastContractPricesStore>()
        .AddSingleton<IEventHandler<ContractLastPriceUpdatedEvt>>(sp => sp.GetRequiredService<LastContractPricesStore>())
        .AddSingleton<IQueryHandler<GetLastKnownContractPrices, IReadOnlyDictionary<int, decimal>>>(sp => sp.GetRequiredService<LastContractPricesStore>())
        .AddSingleton<IQueryHandler<GetLastKnownContractPrice, decimal?>>(sp => sp.GetRequiredService<LastContractPricesStore>())
        .AddSingleton<IQueryHandler<GetConversionRate, decimal?>>(sp => sp.GetRequiredService<LastContractPricesStore>());
}
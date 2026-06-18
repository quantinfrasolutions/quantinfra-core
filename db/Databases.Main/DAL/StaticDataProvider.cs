using Common.StaticData.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Sdk.StaticData;
using Stream = QuantInfra.Sdk.StaticData.Stream;

namespace QuantInfra.Databases.Main.DAL;

public class StaticDataProvider(IServiceProvider serviceProvider) : IStaticDataProvider
{
    public Currency GetCurrency(int id)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return context
            .Currencies
            .Include(c => c.Asset)
            .Include(c => c.BrokerOverrides)
            .AsNoTracking()
            .Single(c => c.CurrencyId == id);
    }

    public TradingSession GetTradingSession(int id)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return context.TradingSessions
            .Include(ts => ts.Exchange)
            .Include(ts => ts.Days)
            .AsNoTracking()
            .Single(ts => ts.TradingSessionId == id);
    }

    public Contract? GetContract(int contractId) => GetContracts([contractId]).SingleOrDefault();
    

    public Contract? GetContractByExternalId(int brokerId, string externalContractId)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return GetContractsQuery(
            context.Contracts.Where(c => c.Template.Broker.BrokerId == brokerId 
                                         && c.ExternalContractId == externalContractId)
            ).SingleOrDefault();
    }

    public IReadOnlyCollection<int> GetFxConversionContractIds()
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return context.FxConversionContracts.Select(c => c.ContractId).ToList();
    }

    public Asset? GetAssetByExternalId(int brokerId, string externalAssetId)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return context.Assets.SingleOrDefault(a => a.Name == externalAssetId);
    }

    public IReadOnlyCollection<Contract> GetContracts(IEnumerable<int> contractIds)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        
        return GetContractsQuery(context.Contracts.Where(c => contractIds.Contains(c.ContractId)))
            .ToList();
    }

    private IQueryable<Contract> GetContractsQuery(IQueryable<Contract> source) => source
        .Include(c => c.Asset)
        .Include(c => c.Template)
            .ThenInclude(t => t.Asset)
        .Include(c => c.Template)
            .ThenInclude(t => t.SettlementCurrency)
                .ThenInclude(c => c.Asset)
        .Include(c => c.Template.SettlementCurrency.BrokerOverrides)
        .Include(c => c.Template)
            .ThenInclude(t => t.QuoteCurrency)
                .ThenInclude(c => c.Asset)
        .Include(c => c.Template)
            .ThenInclude(t => t.BaseCurrency)
                .ThenInclude(c => c.Asset)
        .Include(c => c.Template)
            .ThenInclude(t => t.Exchange)
        .Include(c => c.Template)
            .ThenInclude(t => t.Commissions)
        .Include(c => c.Template)
            .ThenInclude(t => t.TradingSessions)
                .ThenInclude(ts => ts.Days)
        .Include(c => c.Template)
        .ThenInclude(t => t.TradingSessions)
            .ThenInclude(ts => ts.Exchange)
        .Include(c => c.Template)
            .ThenInclude(t => t.DefaultDatafeed)
        .Include(c => c.Template)
            .ThenInclude(t => t.Broker)
        .Include(c => c.Streams.Where(s => s.DatafeedId == s.Contract!.DefaultDatafeedId))
            .ThenInclude(s => s.ConstantStreamValue)
        .AsNoTracking();

    public Stream GetStream(int id)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
            
        return context.Streams
            .Include(s => s.Contract)
                .ThenInclude(c => c!.Template)
            .Include(s => s.Contract)
                .ThenInclude(c => c!.Template)
                    .ThenInclude(t => t.TradingSessions)
                    .ThenInclude(ts => ts.Exchange)
            .Include(s => s.Contract)
                .ThenInclude(c => c!.Template)
                    .ThenInclude(t => t.TradingSessions)
                    .ThenInclude(ts => ts.Days)
                .AsNoTracking()
            .Single(s => s.StreamId == id);
    }

    public (int contractId, bool isDirect) GetFxConversionContract(int fromCcyId, int toCcyId)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        
        var res = context.FxConversionContracts
            .Where(c => 
                c.Contract.Template.BaseCurrency != null && c.Contract.Template.QuoteCurrency != null
                && (
                    (c.Contract.Template.BaseCurrency.CurrencyId == fromCcyId && c.Contract.Template.QuoteCurrency.CurrencyId == toCcyId)
                    || (c.Contract.Template.BaseCurrency.CurrencyId == toCcyId && c.Contract.Template.QuoteCurrency.CurrencyId == fromCcyId)
                )
            )
            .Select(c => new { c.ContractId, isDirect = c.Contract.Template.BaseCurrency!.CurrencyId == fromCcyId })
            .AsNoTracking()
            .Single();

        return (res.ContractId, res.isDirect);
    }

    public Broker? GetBroker(int brokerId)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        return context.Brokers.SingleOrDefault(b => b.BrokerId == brokerId);
    }

    public string? GetContractOrderBookSubscriptionServiceName(int contractId)
    {
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<MainContext>();

        return context.BinanceUsdmOrderBookSubscriptions
            .SingleOrDefault(s => s.ContractId == contractId)?.ClientName;
    }
}
using Common.StaticData.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantInfra.Common.Interfaces.Api.StaticData;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Main.DAL;

public class StaticDataRepository(IServiceProvider serviceProvider) : IStaticDataRepositoryReadOnly
{
    public async Task<IReadOnlyCollection<ContractListView>> GetContractsAsync(ContractsFilter filter)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MainContext>();
        
        filter.Ticker = filter.Ticker?.ToLower();
            
        if (filter?.CommissionId != null)
        {
            throw new NotImplementedException();
            // var contractIds = (await _context.ContractCommissions
            //         .Where(cc => cc.CommissionId == filter.CommissionId.Value).ToListAsync())
            //     .Select(c => c.ContractTemlpateId).ToArray();
            //
            // if (contractIds.Length == 0) return new List<ContractModel>();
            //
            // return await GetContracts(new ContractsFilter
            // {
            //     ContractIds = contractIds,
            //     ExchangeId = filter.ExchangeId
            // });
        }

        return await context
            .Contracts
            .Where(c =>
                (string.IsNullOrEmpty(filter!.Ticker) || c.Ticker.ToLower().Contains(filter.Ticker))
                && (filter.ExchangeId == null || c.Template.Exchange.ExchangeId == filter.ExchangeId.Value)
                && (filter.ContractIds == null || filter.ContractIds.Count == 0 || filter.ContractIds.Contains(c.ContractId))
            )
            .OrderBy(c => c.Ticker)
            .Skip(filter!.Offset)
            .Take(filter.Limit == -1 ? int.MaxValue : filter.Limit)
            .Select(c => new ContractListView(
                c.ContractId, c.Ticker,
                new ContractTemplateListView(c.Template.TemplateId, c.Template.Name, c.Template.SecurityType,
                    c.Template.PlCalculatorType,
                    c.Template.Asset.AssetId, c.Template.Asset.Name, c.Template.MinSize, c.Template.MinSizeMoney,
                    c.Template.MaxSize, c.Template.MaxSizeMoney,
                    c.Template.SizeIncrement, c.Template.TickSize, c.Template.TickValue, c.Template.PriceQuotation,
                    c.Template.SettlementCurrency.CurrencyId,
                    c.Template.SettlementCurrency.Asset.Name, c.Template.BaseCurrency.CurrencyId,
                    c.Template.BaseCurrency.Asset.Name,
                    c.Template.QuoteCurrency.CurrencyId, c.Template.QuoteCurrency.Asset.Name,
                    c.Template.DefaultDatafeed.DatafeedId,
                    c.Template.DefaultDatafeed.Name, c.Template.Exchange.ExchangeId, c.Template.Exchange.Name,
                    c.Template.Broker.BrokerId, c.Template.Broker.Name,
                    c.Template.DaysInYear, c.Template.Description
                ),
                c.Streams.SingleOrDefault(s => s.DatafeedId == c.DefaultDatafeedId),
                c.FirstTradingDate, c.ExpirationDate, c.SyntheticContractType,
                c.SynthRequiresBarRecalculationAtRollover, c.ExternalContractId,
                c.Asset.AssetId, c.Asset.Name, c.Description
            ))
            .AsNoTracking()
            .ToListAsync();
    }

    public Task<IReadOnlyCollection<Currency>> GetCurrenciesAsync(IEnumerable<int> currencyIds)
    {
        throw new NotImplementedException();
    }
}
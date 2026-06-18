using System.Linq;
using QuantInfra.Common.StaticData.Abstractions;

namespace BacktestingCore.Providers;

public class TestStaticDataRepository : InMemoryStaticDataRepository
{
    public void TryAddAsset(Asset asset)
    {
        if (!Assets.ContainsKey(asset.AssetId)) CreateAsset(asset);
    }

    public void TryAddCurrency(Currency currency)
    {
        if (!Currencies.ContainsKey(currency.CurrencyId)) CreateCurrency(currency);
    }

    public void TryAddExchange(Exchange e)
    {
        if (!Exchanges.ContainsKey(e.ExchangeId)) CreateExchange(e);
    }
    
    public void TryAddTradingSession(TradingSession ts)
    {
        if (!TradingSessions.ContainsKey(ts.TradingSessionId)) CreateTradingSession(ts);
    }
    
    public void TryAddCommission(CommissionStructure cs)
    {
        if (!Commissions.ContainsKey(cs.CommissionId)) CreateCommission(cs);
    }
    
    public void TryAddContractTemplate(ContractTemplate template)
    {
        if (!ContractTemplates.ContainsKey(template.TemplateId)) CreateContractTemplate(template);
    }

    public void TryAddContract(Contract c)
    {
        if (!Contracts.ContainsKey(c.ContractId)) CreateContract(c);
    }

    public void TryAddFxConversionContract(Contract contract)
    {
        FxConvesionContracts.Add(contract);
        var baseCcyId = contract.Template.BaseCurrency!.CurrencyId;
        var quoteCcyId = contract.Template.QuoteCurrency!.CurrencyId;
        DirectConversions.TryAdd(baseCcyId, new());
        DirectConversions[baseCcyId].Add(quoteCcyId, new(contract.ContractId, true));
        ReverseConversions.TryAdd(quoteCcyId, new());
        ReverseConversions[quoteCcyId].Add(baseCcyId, new(contract.ContractId, false));
    }
    
    // public void CleanBaseTradeSizes() => BaseTradeSizes.Clear();
    public void TryAddConstantStreamValue(ConstantStreamValue csv)
    {
        ConstantStreams.Add(csv.StreamId, csv);
    }

    public Contract? GetContractForConstantStream(int streamId)
    {
        // TODO: HACK
        return Contracts.Values.SingleOrDefault(c => c?.DefaultStream?.StreamId == streamId);
    }
}
using NodaTime;
using NodaTime.Text;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Common.MarketData.Abstractions;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Backtesting.LocalMarketDataStorage;

/// <summary>
/// Expected file names:
/// * {streamId}_{timeframe}_{timezone}.txt or {streamId}_{timezone}_{timeframe}.csv, e.g. 40257_PT3M_UTC.csv
/// * {streamId}_{timeframe}.parquet, e.g. 40257_PT3M.parquet
/// </summary>
/// 
public class Storage : IMarketDataStorage
{
    private readonly Config _config;

    public Storage(Config config)
    {
        _config = config;
    }

    public bool AllowsRawDataInDifferentTimeframes => true;
    
    public Task<IReadOnlyCollection<RequiredMarketDataUnit>> ValidateRequiredMarketData(
        IReadOnlyCollection<RequiredMarketDataUnit> requiredMarketData, Period? tf = null)
    {
        tf ??= Period.FromMinutes(1);
        var tfStr = PeriodPattern.Roundtrip.Format(tf);
        var res = GetPaths(requiredMarketData, tfStr);
        
        return Task.FromResult((IReadOnlyCollection<RequiredMarketDataUnit>)res.Select(r => (RequiredMarketDataUnit)r).ToList());
    }

    private IReadOnlyCollection<RequiredMarketDataUnitWithPath> GetPaths(IReadOnlyCollection<RequiredMarketDataUnit> requiredMarketData, string? tf = null)
    {
        tf ??= "PT1M";
        return requiredMarketData.Select(md =>
        {
            if (!md.StreamId.HasValue)
            {
                if (md.IsOk)
                {
                    return new RequiredMarketDataUnitWithPath(md, null) { IsOk = false, Message = "No stream specified" };
                }
                return new RequiredMarketDataUnitWithPath(md, null);
            }

            if (!md.DataRequired) return new RequiredMarketDataUnitWithPath(md, null);
            
            foreach (var mdPath in _config.MarketDataPaths)
            {
                var file = Directory.GetFiles(mdPath, $"{md.StreamId}-{tf}-*.csv").SingleOrDefault() 
                    ?? Directory.GetFiles(mdPath, $"{md.StreamId}-{tf}.parquet").SingleOrDefault();

                if (file is not null)
                {
                    return new RequiredMarketDataUnitWithPath(md, file);
                }
            }

            return new RequiredMarketDataUnitWithPath(md, null) { IsOk = false, Message = "Market data file not found" };
        }).ToList();
    }

    public IMarketDataHistoryProvider CreateMarketDataHistoryProvider(
        IReadOnlyCollection<RequiredMarketDataUnit> reqs,
        IReadOnlyDictionary<int, IReadOnlyCollection<TradingSession>>? tradingSessions, 
        Period? tf = null
    )
    {
        tf ??= Period.FromMinutes(1);
        var paths = GetPaths(reqs, PeriodPattern.Roundtrip.Format(tf));
        return new MultipleSourcesMarketDataProvider(paths, _config.DateTimeFormat, tradingSessions);
    }
}
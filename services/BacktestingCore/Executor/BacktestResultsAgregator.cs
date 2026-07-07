using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Domain.Events.Accounts.AccountsService.Primary;
using QuantInfra.Domain.Events.Accounts.AccountsService.Projections;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Positions;

namespace QuantInfra.Services.BacktestingCore.Executor;

internal class BacktestResultsAgregator :
    IProjectionWriter<SharePriceHistoryProjectionEvt>,
    IProjectionWriter<PositionChangedEvt>,
    IProjectionWriter<BalanceMarkedToMarketProjectionEvt>,
    IEventHandler<TradeEvt>,
    IEventHandler<AccountEndOfDayEvt>
{
    private readonly bool _recordPositionCloses, _recordEndOfDayPositions;
    
    private readonly List<SharePriceHistory> _returns;
    public IReadOnlyList<SharePriceHistory> Returns => _returns;
    
    private readonly List<Position> _positionCloses = new(0);
    public IReadOnlyList<Position> PositionCloses => _positionCloses;
    
    private readonly List<PositionValue> _positionValues = new(0);
    public IReadOnlyList<PositionValue> PositionValues => _positionValues;
    
    private readonly List<Position> _endOfDayPositions = new(0);
    public IReadOnlyList<Position> EndOfDayPositions => _endOfDayPositions;
    
    private readonly List<BalanceValue> _endOfDayBalances = new(0);
    public IReadOnlyList<BalanceValue> EndOfDayBalances => _endOfDayBalances;
        
    private readonly List<Trade> _trades = new(0);
    public IReadOnlyList<Trade> Trades => _trades;
    
    private readonly List<Commission> _commissions;

    private readonly int _numberOfDays;
    
    Instant _lastProcessedDt;
        
    public BacktestResultsAgregator(TestExecutorOptions options, PersistOptions persistOptions, int numberOfStrategies)
    {
        _numberOfDays = (int)(options.EndDt - options.StartDt).TotalDays + 1; // initial sp event + one extra day
        if (persistOptions.SaveDailyReturns) _returns = new (numberOfStrategies * _numberOfDays);
        if (persistOptions.SavePositions)
        {
            _recordPositionCloses = true;
            _positionCloses = new(numberOfStrategies * _numberOfDays * persistOptions.ExpectedNumberOfTradesPerDay * 2);
        }
        if (persistOptions.SaveEndOfDayValues)
        {
            _recordEndOfDayPositions = true;
            _endOfDayPositions = new(numberOfStrategies * _numberOfDays * persistOptions.ExpectedNumberOfOpenPositionsAtEndOfDay);
            _endOfDayBalances = new(numberOfStrategies * _numberOfDays * persistOptions.ExpectedNumberOfOpenPositionsAtEndOfDay);
            _positionValues = new(numberOfStrategies * _numberOfDays * persistOptions.ExpectedNumberOfOpenPositionsAtEndOfDay);
        }
        if (persistOptions.SaveTrades) _trades = new(numberOfStrategies * _numberOfDays * persistOptions.ExpectedNumberOfTradesPerDay);
        // _commissions = new List<Commission>(numberOfStrategies * numberOfDays);
    }
    
    // public override void PersistTrade(Trade trade)
    // {
    //     // if (trade.Dt < _lastProcessedDt)
    //     // {
    //     //      Console.WriteLine($"{trade.Dt < _lastProcessedDt} {trade.Dt}  {_lastProcessedDt}");
    //     //     // Debugger.Break();
    //     // }
    //     base.PersistTrade(trade);
    //     _lastProcessedDt = trade.Dt;
    //     Results.SaveTradeRecord(trade);
    //     Results.SaveCommissionRecord(new Commission()
    //     {
    //         TradeId = trade.TradeId,
    //         Amount = trade.Commission
    //     });
    // }

    public void Write(PositionChangedEvt evt)
    {
        if (evt.Timestamp < _lastProcessedDt) Debugger.Break();
        
        if (_recordPositionCloses && evt.Type == PositionChangeType.Close)
        {
            _positionCloses.Add(evt.PositionHistoryRecord!);
            _lastProcessedDt = evt.PositionHistoryRecord!.CloseDt!.Value;
        }

        if (_recordEndOfDayPositions && evt.Type == PositionChangeType.MTM)
        {
            _endOfDayPositions.Add(evt.PositionHistoryRecord!);
            _lastProcessedDt = evt.PositionHistoryRecord!.CloseDt!.Value;
        }
    }
    
    public void Write(BalanceMarkedToMarketProjectionEvt evt)
    {
        _endOfDayBalances.Add(evt.Value);
    }

    public void Write(SharePriceHistoryProjectionEvt evt)
    {
        var sp = evt.SharePrice;
        // if (sp.Dt < _lastProcessedDt) Debugger.Break();
        _lastProcessedDt = sp.Dt;
        var accountId = evt.AccountId;
        // if (!_returns.ContainsKey(accountId)) _returns.Add(accountId, new (_numberOfDays + 1));
        // _returns[accountId].Add(sp);
        _returns.Add(sp);
    }
    
    public void Handle(AccountEndOfDayEvt evt)
    {
        _positionValues.AddRange(evt.PositionValues.Values);
    }

    public void Handle(TradeEvt evt)
    {
        _trades.Add(evt.Trade);
    }
}

internal static class BacktestResultsAggregatorConfigurationExtensions
{
    public static IServiceCollection AddBacktestResultsAggregator(this IServiceCollection sc,
        TestExecutorOptions options, PersistOptions persistOptions, int numberOfStrategies)
    {
        sc.AddSingleton<BacktestResultsAgregator>(sp => new(options, persistOptions, numberOfStrategies));

        if (persistOptions.SaveTrades)
            sc.AddSingleton<IEventHandler<TradeEvt>>(sp => sp.GetRequiredService<BacktestResultsAgregator>());
        
        if (persistOptions.SavePositions || persistOptions.SaveEndOfDayValues)
            sc.AddSingleton<IProjectionWriter<PositionChangedEvt>>(sp => sp.GetRequiredService<BacktestResultsAgregator>());

        if (persistOptions.SaveEndOfDayValues)
        {
            sc
                .AddSingleton<IProjectionWriter<BalanceMarkedToMarketProjectionEvt>>(sp => sp.GetRequiredService<BacktestResultsAgregator>())
                .AddSingleton<IEventHandler<AccountEndOfDayEvt>>(sp => sp.GetRequiredService<BacktestResultsAgregator>());
        }
        
        if (persistOptions.SaveDailyReturns)
            sc.AddSingleton<IProjectionWriter<SharePriceHistoryProjectionEvt>>(sp => sp.GetRequiredService<BacktestResultsAgregator>());

        return sc;
    }
}
using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using NodaTime;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Common.Interfaces.Api;
using QuantInfra.Common.Interfaces.Api.Accounts;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading;
using QuantInfra.Sdk.Trading.Positions;
using QuantInfra.Common.Utils.Collections;

namespace QuantInfra.Backtesting.FileResultsRepository;

public sealed class TestResultsFileReader : ITestResultsRepositoryReadonly
{
    private readonly string _root;

    public TestResultsFileReader(Config config)
    {
        _root = config.WorkingDirectory 
                ?? throw new ArgumentException("WorkingDirectory is not configured.");
    }

    public Task<IReadOnlyCollection<StrategyConfig>> GetStrategies(Guid unitId)
    {
        var path = GetPath(unitId, Helpers.StrategiesFile);
        var result = JsonSerializer.Deserialize<IReadOnlyCollection<StrategyConfig>>(File.ReadAllText(path), JsonOptions.JsonSerializerOptions);
        return Task.FromResult(result ?? Array.Empty<StrategyConfig>());
    }

    public Task<IReadOnlyList<SharePriceHistory>> GetReturns(Guid unitId, int? strategyId = null) 
        => Task.Run(() =>
        {
            var path = GetPath(unitId, Helpers.ReturnsFile);

            var rows = ReadMany<SharePriceHistory>(path)
                .Where(x => strategyId is null || x.AccountId == strategyId)
                .ToList();

            return Task.FromResult<IReadOnlyList<SharePriceHistory>>(rows);
        });

    public Task<IReadOnlyList<Trade>> GetTrades(Guid unitId, TradeFilter filter) => Task.Run(() =>
    {
        var path = GetPath(unitId, Helpers.TradesFile);

        if (!File.Exists(path))
            return Task.FromResult((IReadOnlyList<Trade>)Array.Empty<Trade>());

        return Task.FromResult((IReadOnlyList<Trade>)ReadMany<Trade>(path)
            .Where(x => MatchesTradeFilter(x, filter))
            .OrderBy(x => x.TradeId)
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .ToList()
        );
    });

    public Task<IReadOnlyList<Position>> GetPositions(Guid testUnitId, PositionHistoryFilter filter) => Task.Run(() =>
    {
        var path = GetPath(testUnitId, Helpers.PositionsFile);

        if (!File.Exists(path))
            return Task.FromResult((IReadOnlyList<Position>)Array.Empty<Position>());

        return Task.FromResult((IReadOnlyList<Position>)ReadMany<Position>(path)
            .Where(x => MatchesPositionFilter(x, filter))
            .OrderBy(x => x.CloseTradeId)
            .Skip(filter.Offset)
            .Take(filter.Limit)
            .ToList()
        );
    });

    public Task<IReadOnlyList<BalanceValue>> GetEndOfDayBalances(Guid unitId, AccountEndOfDayBalancesFilter filter) =>
        Task.Run(() =>
        {
            var path = GetPath(unitId, Helpers.EndOfDayBalancesFile);

            if (!File.Exists(path))
                return Task.FromResult((IReadOnlyList<BalanceValue>)Array.Empty<BalanceValueView>());

            return Task.FromResult((IReadOnlyList<BalanceValue>)ReadMany<BalanceValue>(path)
                .Where(x => MatchesAccountEndOfDayBalancesFilter(x, filter))
                .ToList()
            );
        });

    public Task<IReadOnlyList<(Position, PositionValue)>> GetEndOfDayPositions(Guid unitId, AccountEndOfDayPositionsFilter filter) => 
        Task.Run(() =>
        {
            var path = GetPath(unitId, Helpers.EndOfDayPositionsFile);

            var positions = File.Exists(path)
                ? ReadMany<Position>(path)
                    .Where(x => MatchesAccountEndOfDayPositionsFilter(x, filter))
                    .ToList()
                : new List<Position>();

            path = GetPath(unitId, Helpers.EndOfDayPositionValuesFile);
            var positionValues = File.Exists(path)
                ? ReadMany<PositionValue>(path)
                    .Where(x => MatchesAccountEndOfDayPositionsFilter(x, filter))
                    .ToList()
                : new List<PositionValue>();

            return Task.FromResult((IReadOnlyList<(Position, PositionValue)>)positions.FullOuterJoin(
                positionValues,
                p => p.OpenTradeId,
                p => p.PositionId,
                (l, r, k) => (l, r)
            ).ToList());
        });

    private string GetPath(Guid unitId, string fileName) => Helpers.GetPath(_root, unitId, fileName);

    private static IReadOnlyList<T> ReadMany<T>(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"CSV file not found: {path}");

        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, Helpers.CreateCsvConfig());

        csv.Context.TypeConverterCache.AddConverter<Instant>(new InstantCsvConverter());
        csv.Context.TypeConverterCache.AddConverter<LocalDate>(new LocalDateCsvConverter());

        return csv.GetRecords<T>().ToList();
    }

    private static bool MatchesPositionFilter(Position position, PositionHistoryFilter filter)
    {
        var closeDtFrom = filter.CloseDtFrom.FromApiFormat();
        var closeDtTo = filter.CloseDtTo.FromApiFormat();
        var openDtFrom = filter.OpenDtFrom.FromApiFormat();
        var openDtTo = filter.OpenDtTo.FromApiFormat();
        return
            (filter.AccountId is null || position.AccountId == filter.AccountId)
            && (filter.ContractId is null || position.AccountId == filter.ContractId)
            && (filter.TradeId is null || position.AccountId == filter.TradeId)
            && (closeDtFrom is null || position.CloseDt >= closeDtFrom)
            && (closeDtTo is null || position.CloseDt <= closeDtTo)
            && (openDtFrom is null || position.OpenDt >= openDtFrom)
            && (openDtTo is null || position.OpenDt <= openDtTo);

    }

    private static bool MatchesTradeFilter(Trade trade, TradeFilter filter)
    {
        var fromDt = filter.FromDt.FromApiFormat();
        var toDt = filter.ToDt.FromApiFormat();
        return
            (filter.AccountId is null || trade.AccountId == filter.AccountId)
            && (filter.ContractId is null || trade.AccountId == filter.ContractId)
            && (filter.TradeId is null || trade.AccountId == filter.TradeId)
            && (fromDt is null || trade.Dt >= fromDt)
            && (toDt is null || trade.Dt <= toDt);
    }

    private static bool MatchesAccountEndOfDayBalancesFilter(BalanceValue bv, AccountEndOfDayBalancesFilter filter)
    {
        var fromDt = filter.FromDt.FromApiFormat();
        var toDt = filter.ToDt.FromApiFormat();
        return
            bv.AccountId == filter.AccountId
            && (fromDt is null || bv.Dt >= fromDt)
            && (toDt is null || bv.Dt <= toDt);
    }
    
    private static bool MatchesAccountEndOfDayPositionsFilter(Position position, AccountEndOfDayPositionsFilter filter)
    {
        var dt = filter.Dt.FromApiFormat();
        return position.AccountId == filter.AccountId && position.CloseDt == dt;
    }
    
    private static bool MatchesAccountEndOfDayPositionsFilter(PositionValue position, AccountEndOfDayPositionsFilter filter)
    {
        var dt = filter.Dt.FromApiFormat();
        return position.AccountId == filter.AccountId && position.Dt == dt;
    }
}
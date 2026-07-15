using NodaTime;
using QuantInfra.Backtesting.LocalMarketDataStorage;
using QuantInfra.Common.Backtesting.Abstractions;

namespace LocalMarketDataStorage.Tests;

public class Tests
{
    private readonly Storage _storage = new(new()
    {
        MarketDataPaths = [Directory.GetCurrentDirectory()],
        DateTimeFormat = "uuuu'-'MM'-'dd'T'HH':'mm':'ss",
    });

    
    [Test]
    public async Task TestValidateSingleReqByStreamId()
    {
        var reqs = new List<RequiredMarketDataUnit>
        {
            new()
            {
                ContractId = 12345,
                StreamId = 10000,
                DataRequired = true,
                IsOk = true,
            }
        };
        var tf = Period.FromMinutes(1);
        var res = await _storage.ValidateRequiredMarketData(reqs, tf);
        
        Assert.That(res.Count, Is.EqualTo(1));
        Assert.That(res.Single().IsOk, Is.True);
    }
    
    [Test]
    public async Task TestValidateSingleReqByStreamIdWithTimeZone()
    {
        var reqs = new List<RequiredMarketDataUnit>
        {
            new()
            {
                ContractId = 12345,
                StreamId = 10000,
                DataRequired = true,
                IsOk = true,
            }
        };
        var tf = Period.FromMinutes(5);
        var res = await _storage.ValidateRequiredMarketData(reqs, tf);
        
        Assert.That(res.Count, Is.EqualTo(1));
        Assert.That(res.Single().IsOk, Is.True);
    }
    
    [Test]
    public async Task TestValidateSingleReqByStreamIdParquet()
    {
        var res = await _storage.ValidateRequiredMarketData(
            [
                new()
                {
                    ContractId = 12345,
                    StreamId = 10005,
                    DataRequired = true,
                    IsOk = true,
                }
            ], Period.FromMinutes(1)
        );
        
        Assert.That(res.Count, Is.EqualTo(1));
        Assert.That(res.Single().IsOk, Is.True);
    }
    
    [Test]
    public async Task TestValidateSingleReqNonExistentStreamId()
    {
        var res = await _storage.ValidateRequiredMarketData(
            [
                new()
                {
                    ContractId = 12345,
                    StreamId = 120000,
                    DataRequired = true,
                    IsOk = true,
                }
            ], Period.FromMinutes(1)
        );
        
        Assert.That(res.Count, Is.EqualTo(1));
        Assert.That(res.Single().IsOk, Is.False);
    }

    [Test]
    public async Task TestThrowsOnInvalidStreamOrContractId()
    {
        var reqs = new List<RequiredMarketDataUnit>
        {
            new()
            {
                ContractId = 12345,
                StreamId = 10000,
                DataRequired = true,
                IsOk = true,
            }
        };
        var tf = Period.FromMinutes(1);

        var provider = _storage.CreateMarketDataHistoryProvider(reqs, null, tf);

        Assert.Throws<KeyNotFoundException>(() => provider.GetBAUsByStream(20000,
            Instant.FromUtc(2025, 1, 1, 0, 0),
            Instant.FromUtc(2025, 1, 1, 1, 0)
        ));
        
        Assert.Throws<KeyNotFoundException>(() => provider.GetAggregatedBausByStream(20000,
            Instant.FromUtc(2025, 1, 1, 0, 0),
            Instant.FromUtc(2025, 1, 1, 1, 0),
            Period.FromHours(1), "UTC"
        ));

        Assert.Throws<KeyNotFoundException>(() => provider.GetBAUsByContract(20000,
            Instant.FromUtc(2025, 1, 1, 0, 0),
            Instant.FromUtc(2025, 1, 1, 1, 0)
        ));
        
        Assert.Throws<KeyNotFoundException>(() => provider.GetAggregatedCandlesByContract(20000,
            Instant.FromUtc(2025, 1, 1, 0, 0),
            Instant.FromUtc(2025, 1, 1, 1, 0),
            Period.FromHours(1), "UTC"
        ));
    }

    [Test]
    public async Task TestRetrieveDataFromCsv()
    {
        var reqs = new List<RequiredMarketDataUnit>
        {
            new()
            {
                ContractId = 12345,
                StreamId = 10000,
                DataRequired = true,
                IsOk = true,
            }
        };
        var tf = Period.FromMinutes(1);

        var provider = _storage.CreateMarketDataHistoryProvider(reqs, null, tf);

        var data = provider.GetBAUsByStream(
            10000,
            Instant.FromUtc(2025, 01, 01, 00, 03),
            Instant.FromUtc(2025, 01, 01, 00, 10)
        ).ToList();
        Assert.That(data.Count, Is.EqualTo(7));
        Assert.That(data[0].OpenDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 00, 03)));
        Assert.That(data[0].CloseDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 00, 04)));
        
        data = provider.GetBAUsByContract(
            12345,
            Instant.FromUtc(2025, 01, 01, 00, 03),
            Instant.FromUtc(2025, 01, 01, 00, 10)
        ).ToList();
        Assert.That(data.Count, Is.EqualTo(7));
        Assert.That(data[0].OpenDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 00, 03)));
        Assert.That(data[0].CloseDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 00, 04)));
        
        
        // Currently csv storage doesn't do aggregation, so BAUs are returned for aggregated requests
        data = provider.GetAggregatedBausByStream(
            10000,
            Instant.FromUtc(2025, 01, 01, 00, 03),
            Instant.FromUtc(2025, 01, 01, 00, 10),
            Period.FromHours(1), "UTC"
        ).ToList();
        Assert.That(data.Count, Is.EqualTo(7));
        Assert.That(data[0].OpenDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 00, 03)));
        Assert.That(data[0].CloseDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 00, 04)));
        
        data = provider.GetAggregatedCandlesByContract(
            12345,
            Instant.FromUtc(2025, 01, 01, 00, 03),
            Instant.FromUtc(2025, 01, 01, 00, 10),
            Period.FromHours(1), "UTC"
        ).ToList();
        Assert.That(data.Count, Is.EqualTo(7));
        Assert.That(data[0].OpenDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 00, 03)));
        Assert.That(data[0].CloseDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 00, 04)));
    }
    
    [Test]
    public async Task TestRetrieveDataFromParquet()
    {
        var reqs = new List<RequiredMarketDataUnit>
        {
            new()
            {
                ContractId = 12345,
                StreamId = 10005,
                DataRequired = true,
                IsOk = true,
            }
        };
        var tf = Period.FromMinutes(1);

        var provider = _storage.CreateMarketDataHistoryProvider(reqs, null, tf);

        var data = provider.GetBAUsByStream(
            10005,
            Instant.FromUtc(2025, 01, 01, 00, 03),
            Instant.FromUtc(2025, 01, 01, 00, 10)
        ).ToList();
        Assert.That(data.Count, Is.EqualTo(7));
        Assert.That(data[0].OpenDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 00, 03)));
        Assert.That(data[0].CloseDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 00, 04)));
        
        data = provider.GetBAUsByContract(
            12345,
            Instant.FromUtc(2025, 01, 01, 00, 03),
            Instant.FromUtc(2025, 01, 01, 00, 10)
        ).ToList();
        Assert.That(data.Count, Is.EqualTo(7));
        Assert.That(data[0].OpenDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 00, 03)));
        Assert.That(data[0].CloseDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 00, 04)));
        
        
        data = provider.GetAggregatedBausByStream(
            10005,
            Instant.FromUtc(2025, 01, 01, 01, 00),
            Instant.FromUtc(2025, 01, 01, 02, 00),
            Period.FromHours(1), "UTC"
        ).ToList();
        Assert.That(data.Count, Is.EqualTo(1));
        Assert.That(data[0].OpenDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 01, 00)));
        Assert.That(data[0].CloseDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 02, 00)));
        
        data = provider.GetAggregatedCandlesByContract(
            12345,
            Instant.FromUtc(2025, 01, 01, 01, 00),
            Instant.FromUtc(2025, 01, 01, 02, 00),
            Period.FromHours(1), "UTC"
        ).ToList();
        Assert.That(data.Count, Is.EqualTo(1));
        Assert.That(data[0].OpenDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 01, 00)));
        Assert.That(data[0].CloseDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 02, 00)));
    }

    [Test]
    public async Task TestRetrieveDataFromCsvInAnotherTimezone()
    {
        var reqs = new List<RequiredMarketDataUnit>
        {
            new()
            {
                ContractId = 12345,
                StreamId = 10000,
                DataRequired = true,
                IsOk = true,
            }
        };
        var tf = Period.FromMinutes(5);

        var provider = _storage.CreateMarketDataHistoryProvider(reqs, null, tf);

        var data = provider.GetBAUsByStream(
            10000,
            Instant.FromUtc(2025, 01, 01, 05, 10),
            Instant.FromUtc(2025, 01, 01, 05, 30)
        ).ToList();
        Assert.That(data.Count, Is.EqualTo(4));
        Assert.That(data[0].OpenDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 05, 10)));
        Assert.That(data[0].CloseDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 05, 15)));

        data = provider.GetBAUsByContract(
            12345,
            Instant.FromUtc(2025, 01, 01, 05, 10),
            Instant.FromUtc(2025, 01, 01, 05, 30)
        ).ToList();
        Assert.That(data.Count, Is.EqualTo(4));
        Assert.That(data[0].OpenDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 05, 10)));
        Assert.That(data[0].CloseDt, Is.EqualTo(Instant.FromUtc(2025, 01, 01, 05, 15)));
    }
}
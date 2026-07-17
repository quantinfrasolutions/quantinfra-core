using Microsoft.EntityFrameworkCore;
using QuantInfra.Databases.MarketDataHistory.Models;
using QuantInfra.Sdk.MarketData;

namespace QuantInfra.Databases.MarketDataHistory;

public class MDTimescaleContextDesign : DbContext
{
    private readonly MDDatasource? _dataSource;
    private readonly Config? _config;


    public MDTimescaleContextDesign(MDDatasource? dataSource = null, Config? config = null)
    {
        _dataSource = dataSource;
        _config = config;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var config = _config ?? new();
        var datasource = _dataSource ?? ConfigurationExtensions.GetDataSource(config);
        ConfigurationExtensions.ConfigureOptions(optionsBuilder, datasource.DataSource, config);
    }


    public DbSet<TimeBAU> TimeBAU { get; set; } = default!;
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        Models.TimeBAU.CreateRelations(modelBuilder);
    }


    protected void AppendTimeBAU(ExchangeBar bau) =>
        Database.ExecuteSqlRaw(GetAppendTimeBAUSql(bau));    

    protected async Task AppendTimeBAUAsync(ExchangeBar bar) =>
        await Database.ExecuteSqlRawAsync(GetAppendTimeBAUSql(bar));    

    string GetAppendTimeBAUSql(ExchangeBar bau) =>
        $"""
            INSERT INTO data.time_bau(
                stream_id,
                open_dt,
                close_dt,
                open,
                high,
                low,
                close,
                face_volume,
                dollar_value,
                trading_session_id
            )
            VALUES (
                {bau.StreamId},
                '{bau.OpenDt}',
                '{bau.CloseDt}',
                {bau.Open.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                {bau.High.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                {bau.Low.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                {bau.Close.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                {bau.Volume.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                {bau.DollarValue.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                {(bau.TradingSessionId == null ? "null" : bau.TradingSessionId.ToString())}
            );
        """;

    public void AppendBAU(ExchangeBar bar, BarAggregationType aggType)
    {
        switch (aggType)
        {
            case BarAggregationType.Time:
                AppendTimeBAU(bar);
                break;
            case BarAggregationType.Volume:
                throw new NotImplementedException();
                break;
            default:
                throw new ArgumentException(nameof(aggType));
                break;
        }
    }

    public async Task AppendBAUAsync(ExchangeBar bar, BarAggregationType aggType)
    {
        switch (aggType)
        {
            case BarAggregationType.Time:
                await AppendTimeBAUAsync(bar);
                break;
            case BarAggregationType.Volume:
                throw new NotImplementedException();
                break;
            default:
                throw new ArgumentException();
                break;
        }
    }
}
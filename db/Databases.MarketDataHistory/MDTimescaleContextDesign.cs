using Databases.MarketDataHistory.Models;
using Databases.MarketDataHistory.Models.CorporateEvents;
using Microsoft.EntityFrameworkCore;
using QuantInfra.Databases.MarketDataHistory;
using QuantInfra.Sdk.MarketData;

namespace Databases.MarketDataHistory;

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
    public DbSet<FaceVolumeBAU> VolumeBAU { get; set; } = default!;
    public DbSet<DollarValueBAU> DollarValueBAU { get; set; } = default!;
    public DbSet<Dividend> Dividends { get; set; } = default!;
    public DbSet<Split> Splits { get; set; } = default!;
    public DbSet<RollingContract> RollingContracts { get; set; } = default!;

    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
    //     OnConfiguring(optionsBuilder, _config);
    //
    // public static void OnConfiguring(DbContextOptionsBuilder optionsBuilder, Config config)
    // {
    //     optionsBuilder
    //         .UseNpgsql(
    //             new NpgsqlConnectionStringBuilder
    //             {
    //                 Host = config.Host,
    //                 Port = config.Port,
    //                 Username = config.User,
    //                 Password = config.Password,
    //                 Database = config.Database,
    //                 IncludeErrorDetail = config.IncludeErrorDetail,
    //                 MaxPoolSize = config.MaxPoolSize,
    //                 Timeout = config.ConnectionTimeoutSec,
    //                 CommandTimeout = config.CommandTimeoutSec,
    //                 InternalCommandTimeout = config.CommandTimeoutSec,
    //             }.ConnectionString + $";{config.ConnectionStringExtras}",
    //             //$"Server={config.Host};User Id={config.User};Password={config.Password};Database={config.Database};Include Error Detail={config.IncludeErrorDetail};Maximum Pool Size={config.MaxPoolSize};Timeout={config.ConnectionTimeoutSec};Command Timeout={config.ConnectionTimeoutSec};{config.ConnectionStringExtras}",
    //             o => o.UseNodaTime()
    //         );
    //
    //     if (config.IncludeErrorDetail)
    //     {
    //         optionsBuilder.EnableSensitiveDataLogging();
    //     }
    //
    //     if (config.EnableLowLevelLogging)
    //     {
    //         optionsBuilder
    //             .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Trace);
    //     }
    // }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddCorporateEventsRelations();
        Models.TimeBAU.CreateRelations(modelBuilder);
    }


    protected void AppendTimeBAU(ExchangeBar bau) =>
        Database.ExecuteSqlRaw(GetAppendTimeBAUSql(bau));    

    protected async Task AppendTimeBAUAsync(ExchangeBar bar) =>
        await Database.ExecuteSqlRawAsync(GetAppendTimeBAUSql(bar));    

    string GetAppendTimeBAUSql(ExchangeBar bau) =>
        $"""
            INSERT INTO public.time_bau(
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


    //public void AppendFaceVolumeBAU(ExchangeBar bau)
    //{
    //    // TODO: dollar value
    //    var sql = $"""
    //        INSERT INTO public.face_volume_bau(
    //        open_dt, close_dt, stream, open, high, low, close, face_volume, dollar_value)
    //        VALUES ('{bau.OpenDt}', '{bau.CloseDt}', '{bau.UniqueTicker}', {bau.Open}, {bau.High}, {bau.Low}, {bau.Close}, {bau.Volume}, 0);
    //    """;
    //    this.Database.ExecuteSqlRaw(sql);
    //}

    //public void AppendDollarValueBAU(ExchangeBar bau)
    //{
    //    // TODO: dollar value
    //    var sql = $"""
    //        INSERT INTO public.dollar_value_bau(
    //        open_dt, close_dt, stream, open, high, low, close, face_volume, dollar_value)
    //        VALUES ('{bau.OpenDt}', '{bau.CloseDt}', '{bau.UniqueTicker}', {bau.Open}, {bau.High}, {bau.Low}, {bau.Close}, {bau.Volume}, 0);
    //    """;
    //    this.Database.ExecuteSqlRaw(sql);
    //}

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
                throw new ArgumentException();
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
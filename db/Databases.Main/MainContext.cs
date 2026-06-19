using Microsoft.EntityFrameworkCore;
using Npgsql;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Connectors.Ibkr.Interfaces;
using QuantInfra.Databases.Main.Models.Entities;
using QuantInfra.Databases.Main.Models.Events;
using QuantInfra.Databases.Main.Models.History;
using QuantInfra.Databases.Main.Models.Infrastructure;
using QuantInfra.Databases.Main.Models.Projections;
using QuantInfra.Databases.Main.Models.StaticData;
using QuantInfra.Sdk.Accounting;
using QuantInfra.Sdk.StaticData;
using QuantInfra.Sdk.Trading.Positions;
using Stream = QuantInfra.Sdk.StaticData.Stream;
using TradingClientConfig = QuantInfra.Sdk.Accounts.TradingClientConfig;

namespace QuantInfra.Databases.Main;

public partial class MainContext : DbContext
{
    private readonly NpgsqlDataSource? _dataSource;
    private readonly Config? _config;

    public MainContext(NpgsqlDataSource? dataSource = null, Config? config = null)
    {
        _dataSource = dataSource;
        _config = config;
    } 

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var config = _config ?? new();
        var datasource = _dataSource ?? ConfigurationExtensions.GetDataSource(config);
        ConfigurationExtensions.ConfigureOptions(optionsBuilder, datasource, config);
    }

    public DbSet<LocationModel> Locations { get; set; }
    public DbSet<AccountServiceInstanceModel> AccountServiceInstances { get; set; }
    public DbSet<StrategiesServiceInstanceModel> StrategyServiceInstances { get; set; }
    public DbSet<ExecutionServiceInstanceModel> ExecutionServiceInstances { get; set; }
    
    public DbSet<AccountModel> Accounts { get; set; }
    public DbSet<TradingClientConfig> TradingClients { get; set; }
    public DbSet<SubaccountModel> Subaccounts { get; set; }
    public DbSet<StrategyModel> Strategies { get; set; }
    
    #region Static data
    
    public DbSet<Asset> Assets { get; set; }
    public DbSet<Broker> Brokers { get; set; }
    public DbSet<CommissionStructure> Commissions { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<ContractTemplate> ContractTemplates { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<CurrencyOverride> CurrencyOverrides { get; set; }
    public DbSet<Datafeed> Datafeeds { get; set; }
    public DbSet<Exchange> Exchanges { get; set; }
    public DbSet<FxConversionContract> FxConversionContracts { get; set; }
    public DbSet<Stream> Streams { get; set; }
    public DbSet<ConstantStreamValue> ConstantStreams { get; set; }
    public DbSet<TradingSession> TradingSessions { get; set; }
    public DbSet<TradingSessionInterval> TradingSessionIntervals { get; set; }
    
    #endregion

    #region Market data

    public DbSet<MarketDataClientInstanceModel> MarketDataClients { get; set; }
    public DbSet<BinanceUsdmMarketDataSubscription> BinanceUsdmMarketDataSubscriptions { get; set; }
    public DbSet<BinanceUsdmOrderBookSubscription> BinanceUsdmOrderBookSubscriptions { get; set; }
    public DbSet<IbkrMarketDataSubscription> IbkrMarketDataSubscriptions { get; set; }

    #endregion
    
    #region History
    public DbSet<BalanceOperation> BalanceOperations { get; set; }
    public DbSet<ExecutionReportModel> OrdersHistory { get; set; }
    public DbSet<TradeModel> Trades { get; set; }
    #endregion
    
    #region Events
    
    public DbSet<Event> Events { get; set; }
    public DbSet<PositionValue> EndOfDayPositions { get; set; }
    public DbSet<BalanceValueModel> EndOfDayBalances { get; set; }
    public DbSet<ShareCountUpdate> ShareCountUpdates { get; set; }
    public DbSet<SharePriceUpdate> SharePriceUpdates { get; set; }
    public DbSet<ExternalTradeModel> ExternalTrades { get; set; }
    
    #endregion
    
    #region Projections
    public DbSet<SharePriceHistory> SharePriceHistory { get; set; }
    public DbSet<PositionHistoryModel> PositionsHistory { get; set; }

    #endregion
    
    public NpgsqlDataSource? DataSource => _dataSource;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MainContext).Assembly);
        
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .AddEntitiesRelations()
            .AddInfrastructureRelations()
            .AddStaticDataRelations()
            .AddEventsRelations()
            .AddProjectionsRelations();
    }
}
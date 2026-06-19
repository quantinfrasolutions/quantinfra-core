using System.Text.Json;
using Common.StaticData.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Npgsql;
using QuantInfra.Common.Accounts.Abstractions;
using QuantInfra.Common.EventSourcing;
using QuantInfra.Common.Infrastructure.Abstractions;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Common.Trading.Infrastructure;
using QuantInfra.Connectors.Binance.Common;
using QuantInfra.Connectors.Ibkr.Interfaces;
using QuantInfra.Databases.Main.DAL;
using QuantInfra.Domain.Events.Persistence;
using QuantInfra.Services.AccountsCore.State;

namespace QuantInfra.Databases.Main
{
    public static class ConfigurationExtensions
	{
		public static IServiceCollection ConfigureMainDb(
			this IServiceCollection sc,
			IConfiguration configuration,
			string sectionName = "db.main",
			string? key = null
		)
		{
			sc
				.Configure<Config>(conf =>
					configuration.GetSection(sectionName).Bind(conf)
				)
				.AddSingleton(sp => sp.GetService<IOptions<Config>>()!.Value);
				
			if (string.IsNullOrEmpty(key)) 
				sc.AddSingleton<NpgsqlDataSource>(sp => GetDataSource(sp.GetService<Config>()!));
			else 
				sc.AddKeyedSingleton<NpgsqlDataSource>(key, (sp, _) => GetDataSource(sp.GetService<Config>()!));

			return sc;
		}

		public static IServiceCollection ConfigureMainDb(this IServiceCollection sc, Config config) => sc
			.AddSingleton(config)
			.AddSingleton<NpgsqlDataSource>(sp => GetDataSource(sp.GetService<Config>()!));
		
		public static IServiceCollection AddMainDbContext(this IServiceCollection sc) => sc
			.AddDbContext<MainContext>();
		
		public static IServiceCollection ConfigureMainDb(this IServiceCollection sc, NpgsqlDataSource dataSource) => sc
			.AddSingleton<NpgsqlDataSource>(dataSource);
		
		public static IServiceCollection ConfigureMainDb(this IServiceCollection sc, NpgsqlDataSource dataSource, string key) => sc
			.AddKeyedSingleton<NpgsqlDataSource>(key, dataSource);

		public static IServiceCollection UseMainDbAccountRecordsRepository(this IServiceCollection sc) => sc
			.AddScoped<IAccountRecordsRepository>(sp => sp.GetRequiredService<MainContext>());
		
		public static IServiceCollection UseSingletonMainDbAccountRecordsRepositoryReadonly(this IServiceCollection sc) => sc
			.AddSingleton<AccountRecordsRepositoryReadonly>()
			.AddSingleton<IAccountRecordsRepositoryReadonly>(sc => sc.GetRequiredService<AccountRecordsRepositoryReadonly>());
		
		public static IServiceCollection UseMainDbStrategyRecordsRepository(this IServiceCollection sc) => sc
			.AddScoped<IStrategyRecordsRepository>(sp => sp.GetRequiredService<MainContext>());
		
		public static IServiceCollection UseSingletonMainDbStrategyRecordsRepositoryReadonly(this IServiceCollection sc) => sc
			.AddSingleton<StrategyRecordsRepositoryReadonly>()
			.AddSingleton<IStrategyRecordsRepositoryReadonly>(sp => sp.GetRequiredService<StrategyRecordsRepositoryReadonly>());

		public static IServiceCollection UseMainDbStaticDataProvider(this IServiceCollection sc) => sc
			.AddSingleton<IStaticDataProvider, StaticDataProvider>();
		
		public static IServiceCollection UseMainDbEventsRepository(this IServiceCollection sc) => sc
			.AddScoped<EventPersister>()
			.AddSingleton<EventPersisterFactory>()
			.AddSingleton<IEventPersisterFactory>(sp => sp.GetRequiredService<EventPersisterFactory>());

		public static IServiceCollection UseMainDbMarketDataServiceStreamsRepository(this IServiceCollection sc) => sc
			.AddSingleton<MarketDataServiceStreamsRepository>()
			.AddSingleton<IMarketDataServiceStreamsRepository>(sp => sp.GetRequiredService<MarketDataServiceStreamsRepository>());

		public static IServiceCollection UseMainDbBinanceActiveSubscriptionsRepository(this IServiceCollection sc) => sc
			.AddSingleton<IBinanceActiveSubscriptionsRepository, BinanceActiveSubscriptionsRepository>();
		
		public static IServiceCollection UseMainDbBinanceOrderBookSubscriptionsRepository(this IServiceCollection sc) => sc
			.AddSingleton<IBinanceOrderBookSubscriptionsRepository, BinanceUsdmOrderBookSubscriptionsRepository>();
		
		public static IServiceCollection UseMainDbIbkrActiveSubscriptionsRepository(this IServiceCollection sc) => sc
			.AddSingleton<IIbkrActiveSubscriptionsRepository, IbkrActiveSubscriptionsRepository>();
		
		public static IServiceCollection UseMainDbPersistentEventStorage(this IServiceCollection sc) => sc
			.AddSingleton<PersistentEventStorage>()
			.AddSingleton<IPersistentEventStorage<AccountServiceState>>(sp => sp.GetRequiredService<PersistentEventStorage>());
		
		public static IServiceCollection UseTradingAccountsRepositoryReadonly(this IServiceCollection sc) => sc
			.AddSingleton<TradingAccountsRepository>()
			.AddSingleton<ITradingAccountsRepositoryReadonly>(sp => sp.GetRequiredService<TradingAccountsRepository>());
		
		public static IServiceCollection UseTradingAccountsRepository(this IServiceCollection sc) => sc
			.AddSingleton<TradingAccountsRepository>()
			.AddSingleton<ITradingAccountsRepositoryReadonly>(sp => sp.GetRequiredService<TradingAccountsRepository>())
			.AddSingleton<ITradingAccountsRepository>(sp => sp.GetRequiredService<TradingAccountsRepository>());
		
		public static IServiceCollection UseMainDbInfrastructureRepository(this IServiceCollection sc) => sc
			.AddSingleton<InfrastructureRepository>()
			.AddSingleton<IInfrastructureRepositoryReadonly>(sp => sp.GetRequiredService<InfrastructureRepository>())
			.AddSingleton<IInfrastructureRepository>(sp => sp.GetRequiredService<InfrastructureRepository>());
		
		// public static IServiceCollection AddMainDbContextFactory(this IServiceCollection sc) => sc
		// 	.AddDbContextFactory<MainContext>();
		//
		// public static IServiceCollection UseIbkrActiveSubscriptionsRepository(this IServiceCollection sc) =>
		// 	sc.AddScoped<IIbkrActiveSubscriptionsRepository>(sp => sp.GetService<MainContext>()!);
		//
		// public static IServiceCollection UseBinanceActiveSubscriptionsRepository(this IServiceCollection sc) =>
		// 	sc.AddScoped<IBinanceActiveSubscriptionsRepository>(sp => sp.GetService<MainContext>()!);

		internal static void ConfigureOptions(DbContextOptionsBuilder optionsBuilder, NpgsqlDataSource dataSource, Config config)
		{
			optionsBuilder.UseNpgsql(dataSource, o =>
			{
				o.UseNodaTime();
				o.MigrationsHistoryTable("migrations_history", "public");
			});

			if (config.IncludeErrorDetail)
			{
				optionsBuilder.EnableSensitiveDataLogging();
			}

			if (config.EnableLowLevelLogging)
			{
				optionsBuilder
					.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Trace);
			}
		}

		internal static NpgsqlDataSource GetDataSource(Config config)
		{
			var dataSource = new NpgsqlDataSourceBuilder(
					new NpgsqlConnectionStringBuilder
					{
						Host = config.Host,
						Port = config.Port,
						Username = config.User,
						Password = config.Password,
						Database = config.Database,
						IncludeErrorDetail = config.IncludeErrorDetail,
						MinPoolSize = config.MinPoolSize,
						MaxPoolSize = config.MaxPoolSize,
						Timeout = config.ConnectionTimeoutSec,
						CommandTimeout = config.CommandTimeoutSec,
					}.ConnectionString + $";{config.ConnectionStringExtras}"
				)
				.UseNodaTime()
				.EnableDynamicJson()
				.ConfigureJsonOptions(new JsonSerializerOptions().ConfigureForNodaTime(DateTimeZoneProviders.Tzdb))
				.Build();

			return dataSource;
		}
	}
}


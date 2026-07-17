using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace QuantInfra.Databases.MarketDataHistory
{
	public static class ConfigurationExtensions
	{
		public static IServiceCollection ConfigureMarketDataHistoryDb(
			this IServiceCollection services,
			IConfiguration configuration,
			string sectionName = "db.mds"
		) => services
			.Configure<Config>(
				conf => configuration.GetSection(sectionName).Bind(conf)
			)
			.AddSingleton(sp => sp.GetService<IOptions<Config>>()?.Value ?? new Config())
			.AddSingleton<MDDatasource>(sp => GetDataSource(sp.GetService<Config>()!));
		
		public static IServiceCollection ConfigureMarketDataHistoryDb(this IServiceCollection sc, MDDatasource dataSource) => sc
			.AddSingleton<MDDatasource>(dataSource);

		public static IServiceCollection AddMarketDataHistoryDbContext(this IServiceCollection services) => services
			.AddDbContext<MDTimescaleContextDesign>()
			.AddDbContext<MDTimescaleContext>();
			// .AddScoped<MDTimescaleContext>()
			// .AddScoped<MDTimescaleContextDesign>(sp => sp.GetRequiredService<MDTimescaleContext>());
		
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
		
		internal static MDDatasource GetDataSource(Config config)
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
						MaxPoolSize = config.MaxPoolSize,
						Timeout = config.ConnectionTimeoutSec,
						CommandTimeout = config.CommandTimeoutSec,
					}.ConnectionString + $";{config.ConnectionStringExtras}"
				)
				.UseNodaTime()
				.Build();

			return new(dataSource);
		}
	}

	public class MDDatasource(NpgsqlDataSource dataSource)
	{
		public NpgsqlDataSource DataSource { get; init; } = dataSource;
	}
}


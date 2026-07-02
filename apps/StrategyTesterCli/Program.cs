using ConsoleAppFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using QuantInfra.Backtesting.FileResultsRepository;
using QuantInfra.Core.Apps.StrategyTesterCli;
using QuantInfra.Databases.Backtesting.Sqlite;
using QuantInfra.Databases.Main;
using QuantInfra.Services.LocalTestServer;

var app = ConsoleApp.Create()
    
    .ConfigureGlobalOptions((ref conf) =>
    {
        var env = conf.AddGlobalOption<bool>("-e|--env", "Use environment variables");
        var file = conf.AddGlobalOption<string>("-f|--file", "Use config file");
        // var input = conf.AddGlobalOption<bool>("-i|--input", "Use command line arguments");

        var configurationBuilder = new ConfigurationBuilder();
        if (!string.IsNullOrEmpty(file)) configurationBuilder.AddJsonFile(file, optional: false);
        if (env) configurationBuilder.AddEnvironmentVariables();
        // if (input) configurationBuilder.AddCommandLine(args);
        
        return new GlobalOptions(configurationBuilder.Build());
    })

    .ConfigureServices((context, _, services) =>
    {
        // store global-options to DI
        var globalOptions = (GlobalOptions)context.GlobalOptions!;
        var configuration = globalOptions.Configuration;
        
        services
            .AddLogging(c =>
            {
                c.ClearProviders();
                c.AddNLog();
                LogManager.Configuration = new NLogLoggingConfiguration(configuration.GetSection("nlog"));
            })
            .AddSingleton<LoggingConfiguration>(_ => new NLogLoggingConfiguration(configuration.GetSection("nlog-testing")))
            
            .ConfigureSqliteBacktesting(configuration, configureAction: (sp, conf) =>
            {
                var config = sp.GetRequiredService<LocalTestServerConfig>();
                conf.DbPath = Path.Join(config.WorkingDirectory, ".db.sqlite");
            })
            .AddSqliteBacktesting()
            .UseSqliteBacktestingUnitsRepository()
            
            .ConfigureMainDb(configuration)
            .AddMainDbContext()
            .UseMainDbStaticDataProvider()
            
            .ConfigureFileResultsRepository(configuration, configureAction: (sp, conf) =>
            {
                var config = sp.GetRequiredService<LocalTestServerConfig>();
                conf.WorkingDirectory = config.WorkingDirectory;
            })
            .AddFileResultsPersister()
            
            .ConfigureLocalTestServer(configuration)
            .AddLocalTestServer();
    });

app.Add<Commands>();

app.Run(args);
using System.Text.Json;
using System.Text.Json.Serialization;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using NodaTime.Serialization.SystemTextJson;
using QuantInfra.Services.LocalTestServer;

namespace QuantInfra.Core.Apps.StrategyTesterCli;

internal class Commands(LocalTestServer server, IServiceProvider serviceProvider)
{
    /// <summary>
    /// Lists available testing actions
    /// </summary>
    [Command("get-actions")]
    public async Task GetActionsAsync()
    {
        var actions = await server.GetSupportedActionsAsync();
        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(actions, JsonSerializerOptions));
        await Console.Out.FlushAsync();
    }
    
    /// <summary>
    /// Lists available metrics calculators
    /// </summary>
    [Command("get-metrics")]
    public async Task GetMetricsAsync()
    {
        var actions = await server.GetSupportedMetricsCalculatorsAsync();
        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(actions, JsonSerializerOptions));
        await Console.Out.FlushAsync();
    }
    
    /// <summary>
    /// Lists strategy classes found in the supplied dlls
    /// </summary>
    [Command("get-strategies")]
    public async Task GetStrategiesAsync()
    {
        var strategies = await server.GetStrategiesAsync();
        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(strategies, JsonSerializerOptions));
        await Console.Out.FlushAsync();
    }

    /// <summary>
    /// Returns a sample configuration JSON for the chosen command
    /// </summary>
    [Command("get-sample-params")]
    public async Task GetSampleParamsAsync(string actionName)
    {
        var sample = await server.GetSampleActionParamsAsync(actionName);
        await Console.Out.WriteLineAsync(sample);
        await Console.Out.FlushAsync();
    }
    
    /// <summary>
    /// Returns a sample configuration JSON for the chosen command
    /// </summary>
    [Command("get-sample-metrics-options")]
    public async Task GetSampleMetricsOptions(string calculatorName)
    {
        var sample = await server.GetSampleMetricsCalculatorOptionsAsync(calculatorName);
        await Console.Out.WriteLineAsync(sample);
        await Console.Out.FlushAsync();
    }

    [Command("validate-params")]
    public async Task ValidateParamsAsync(string actionName, string @params)
    {
        var res = await server.ValidateParamsAsync(actionName, @params);
        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(res, JsonSerializerOptions));
        await Console.Out.FlushAsync();
    }
    
    [Command("validate-market-data")]
    public async Task ValidateRequiredMarketData(Guid unitId)
    {
        await Helpers.MigrateSqliteAsync(serviceProvider);
        var res = await server.ValidateRequiredMarketDataAsync(unitId);
        await Console.Out.WriteLineAsync(JsonSerializer.Serialize(res, JsonSerializerOptions));
        await Console.Out.FlushAsync();
    }
    
    [Command("run")]
    public async Task RunAsync(Guid unitId, bool verboseProgress = false, int interval = 1000)
    {
        await Helpers.MigrateSqliteAsync(serviceProvider);
        await server.RunAsync(unitId);
    }

    [Command("view-options")]
    public Task ViewOptionsAsync(string configName)
    {
        object? config = null;
        switch (configName)
        {
            case "db.sqlite":
            {
                config = serviceProvider.GetService<QuantInfra.Databases.Backtesting.Sqlite.Config>();
                Console.WriteLine(config);
            } break;
            
            case "db.main":
            {
                config = serviceProvider.GetService<QuantInfra.Databases.Main.Config>();
                Console.WriteLine(config);
            } break;

            case "fileResults":
            {
                config = serviceProvider.GetService<QuantInfra.Backtesting.FileResultsRepository.Config>();
                Console.WriteLine(config);
            } break;

            case "local-test-server":
            {
                config = serviceProvider.GetService<QuantInfra.Services.LocalTestServer.LocalTestServerConfig>();
                Console.WriteLine(config);
            } break;
        }
        
        Console.WriteLine(JsonSerializer.Serialize(config, ConfigOptions.Value));
        
        return Task.CompletedTask;
    }

    
    
    private static readonly Lazy<JsonSerializerOptions> Options = new(() =>
    {
        var options = new JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString,
            WriteIndented = false,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        options.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    });

    public static JsonSerializerOptions JsonSerializerOptions => Options.Value;
    
    private static readonly Lazy<JsonSerializerOptions> ConfigOptions = new(() =>
    {
        var options = new JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        options.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    });
}
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NodaTime.Serialization.SystemTextJson;
using QuantInfra.Common.Backtesting.Abstractions;
using QuantInfra.Common.Strategies.Abstractions;
using QuantInfra.Sdk.Backtesting;

namespace QuantInfra.Backtesting.LocalTestServerWrapper;

public sealed class Wrapper : ITestServer
{
    private readonly Config _config;
    private readonly ILogger<Wrapper> _logger;

    public Wrapper(Config config, ILogger<Wrapper> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<string>> GetSupportedActionsAsync()
    {
        var process = GetProcess("get-actions");
        var task = process.WaitForExitAsync();
        if (await Task.WhenAny(task, Task.Delay(10000)) != task)
            throw new TimeoutException();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardOutput.ReadToEndAsync();
            _logger.LogError($"Error retrieving actions: {error}");
            throw new Exception(error);
        }
        return (await JsonSerializer.DeserializeAsync<IReadOnlyCollection<string>>(process.StandardOutput.BaseStream, JsonSerializerOptions))!;
    }

    public async Task<IReadOnlyCollection<string>> GetSupportedMetricsCalculatorsAsync()
    {
        var process = GetProcess("get-metrics");
        var task = process.WaitForExitAsync();
        if (await Task.WhenAny(task, Task.Delay(10000)) != task)
            throw new TimeoutException();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardOutput.ReadToEndAsync();
            _logger.LogError($"Error retrieving actions: {error}");
            throw new Exception(error);
        }
        return (await JsonSerializer.DeserializeAsync<IReadOnlyCollection<string>>(process.StandardOutput.BaseStream, JsonSerializerOptions))!;
    }

    public async Task<IReadOnlyCollection<StrategyTypeDescription>> GetStrategiesAsync()
    {
        var process = GetProcess("get-strategies");
        var task = process.WaitForExitAsync();
        if (await Task.WhenAny(task, Task.Delay(1000)) != task)
            throw new TimeoutException();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardOutput.ReadToEndAsync();
            _logger.LogError($"Error retrieving strategies: {error}");
            throw new Exception(error);
        }
        return (await JsonSerializer.DeserializeAsync<List<StrategyTypeDescription>>(process.StandardOutput.BaseStream, JsonSerializerOptions))!;
    }

    public async Task<string> GetSampleActionParamsAsync(string action)
    {
        var process = GetProcess("get-sample-params", "--action-name", action);
        var task = process.WaitForExitAsync();
        if (await Task.WhenAny(task, Task.Delay(1000)) != task)
            throw new TimeoutException();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardOutput.ReadToEndAsync();
            _logger.LogError($"Error getting sample action: {error}");
            throw new Exception(error);
        }
        return await process.StandardOutput.ReadToEndAsync();
    }

    public async Task<string> GetSampleMetricsCalculatorOptionsAsync(string calculator)
    {
        var process = GetProcess("get-sample-metrics-options", "--calculator-name", calculator);
        var task = process.WaitForExitAsync();
        if (await Task.WhenAny(task, Task.Delay(1000)) != task)
            throw new TimeoutException();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardOutput.ReadToEndAsync();
            _logger.LogError($"Error getting sample action: {error}");
            throw new Exception(error);
        }
        return await process.StandardOutput.ReadToEndAsync();
    }

    public async Task<IReadOnlyCollection<RequiredMarketDataUnit>> ValidateRequiredMarketDataAsync(Guid unitId)
    {
        var process = GetProcess("validate-market-data", "--unit-id", unitId.ToString());
        
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardOutput.ReadToEndAsync();
            _logger.LogError($"Error validating market data: {error}");
            throw new Exception(error);
        }
        return (await JsonSerializer.DeserializeAsync<IReadOnlyCollection<RequiredMarketDataUnit>>(process.StandardOutput.BaseStream, JsonSerializerOptions))!;
    }

    public async Task<ActionParamsValidationResult> ValidateParamsAsync(string action, string? options)
    {
        options ??= "null";
        options = options.Replace('\n', ' ');
        options = $"""{options}""";
        var process = GetProcess("validate-params", "--action-name", action, "--params", options);
        var task = process.WaitForExitAsync();
        if (await Task.WhenAny(task, Task.Delay(1000)) != task)
            throw new TimeoutException();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardOutput.ReadToEndAsync();
            return new(false, error);
        }
        return (await JsonSerializer.DeserializeAsync<ActionParamsValidationResult>(process.StandardOutput.BaseStream, JsonSerializerOptions))!;
    }

    public async Task RunAsync(Guid unitId)
    {
        try
        {
            var process = GetProcess("run", "--unit-id", unitId.ToString());
            await _runSemaphore.WaitAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardOutput.ReadToEndAsync();
                throw new Exception(error);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _runSemaphore.Release();
        }
    }

    private SemaphoreSlim _runSemaphore = new(1);
    // private ConcurrentDictionary<Guid, BacktestingUnitStatus> _statuses = new();
    // private Dictionary<Guid, ActionResult> _results = new();
    private ConcurrentDictionary<Guid, Process> _processes = new();
    

    public async Task CancelExecutionAsync(Guid unitId)
    {
        throw new NotImplementedException();
        // var process = _processes.GetValueOrDefault(unitId);
        // if (process is null)
        // {
        //     _statuses.Remove(unitId, out _);
        //     return;
        // }
        //
        // process.Kill();
        // await process.WaitForExitAsync();
        // _statuses.Remove(unitId, out _);
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

    private Process GetProcess(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add(_config.StrategyTesterCliPath);
        
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        if (_config.UseEnv)
        {
            psi.ArgumentList.Add("-e");
        }

        if (!string.IsNullOrEmpty(_config.ConfigFilePath))
        {
            psi.ArgumentList.Add("-f");
            psi.ArgumentList.Add(_config.ConfigFilePath);
        }

        if (_config.Args != null)
        {
            psi.ArgumentList.Add("-i");
            foreach (var arg in _config.Args) psi.ArgumentList.Add(arg);
        }
        
        var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start engine process.");
        Console.WriteLine(string.Join(' ', psi.ArgumentList.Select(s => s.Replace("\"", "\\\""))));
        return p;
    }
}
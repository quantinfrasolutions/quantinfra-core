using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Metrics;
using Disruptor.Dsl;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NodaTime.Text;
using Prometheus;
using QuantInfra.Common.ServiceBase.ServiceMessages;

namespace QuantInfra.Common.ServiceBase.WAL;

public sealed class WalManager<TState>: Disruptor.IEventHandler<IncomingDisruptorMessage>,
    IDisposable
    where TState : class, IState<TState>, new()
{
    private readonly double TickToMicro = 1_000_000.0 / Stopwatch.Frequency;
    
    private readonly WalConfig _config;
    private readonly TState _state;
    private readonly Disruptor<IncomingDisruptorMessage> _inputDisruptor;
    private readonly ILogger _logger;
    private readonly List<Assembly> _transportMessagesAssemblies;
    private readonly ReplayingClock _clock;

    private readonly byte[] _newLine = Encoding.UTF8.GetBytes(System.Environment.NewLine);
    private readonly byte[] _delimiter = Encoding.UTF8.GetBytes("|");
    
    private FileStream _fs;
    private long _currentWalPartition = 0;
    private long _currentWalPartitionStartSequence = 0;
    
    private readonly long _walRollPeriodSeconds;
    public readonly long WalRollPeriodEvents;

    private bool _stopRequested;
    private readonly Histogram? _walWaitTime;
    private readonly Histogram? _walTime;


    public static JsonSerializerOptions WalJsonSerializerOptions { get; } = new()
    {
        WriteIndented = false,
    };

    public static JsonSerializerOptions StateJsonSerializerOptions { get; } = new()
    {
        WriteIndented = true,
    };

    public WalManager(
        WalConfig config,
        TState state,
        Disruptor<IncomingDisruptorMessage> inputDisruptor,
        ILogger<WalManager<TState>> logger,
        ReplayingClock clock,
        List<Assembly> transportMessagesAssemblies
    )
    {
        _config = config;
        _state = state;
        _walRollPeriodSeconds = !string.IsNullOrEmpty(_config.PersistStatePeriod)
            ? (long)PeriodPattern.Roundtrip.Parse(_config.PersistStatePeriod).Value.ToDuration().TotalSeconds
            : 0;
        WalRollPeriodEvents = _config.MaxNumberOfEventsInWal;
        
        _inputDisruptor = inputDisruptor;
        _logger = logger;
        _clock = clock;
        _transportMessagesAssemblies = transportMessagesAssemblies;

        if (config.WritePerformanceMetrics)
        {
            _walWaitTime = MetricsDefinition.GetWalWaitTime(config.ServiceName, config.Monolith,
                config.WalWaitTimeParams[0], config.WalWaitTimeParams[1], config.WalWaitTimeParams[2]);
            _walTime = MetricsDefinition.GetWalTime(config.ServiceName, config.Monolith,
                config.WalTimeParams[0], config.WalTimeParams[1], config.WalTimeParams[2]);
        }
    }

    public void Start()
    {
        _logger.LogInformation("Starting state manager");
        
        if (!Directory.Exists(_config.WorkingDirPath)) Directory.CreateDirectory(_config.WorkingDirPath);
        
        var (lastStateTs, stateFPath) = ScanFiles(_config.WorkingDirPath, "state");
        var (lastWalTs, walFPath) =  ScanFiles(_config.WorkingDirPath, "wal");
        
        if (!lastStateTs.HasValue && !lastWalTs.HasValue)
        {
            if (_config.TryRetrieveStateFromPersistentStorage) throw new WalDirectoryEmptyException();
            else
            {
                var now = _clock.AbsoluteClock.GetCurrentInstant().ToUnixTimeMilliseconds();

                lastWalTs = now;
                walFPath = Path.Combine(_config.WorkingDirPath, $"{now}.wal");
                _logger.LogInformation($"Created wal file {walFPath}");
                using var walFile = File.Create(walFPath);

                lastStateTs = now;
                var state = new TState();
                stateFPath = Path.Combine(_config.WorkingDirPath, $"{now}.state");
                using var stateFile = File.OpenWrite(stateFPath);
                JsonSerializer.Serialize(stateFile, state, JsonSerializerOptions);
                stateFile.Flush();
                _logger.LogInformation($"Created empty state file {stateFPath}");
            }
        }
        
        if (lastWalTs.HasValue && lastWalTs == lastStateTs)
        {
            _currentWalPartition = lastWalTs.Value;
            _logger.LogInformation($"Using partition {_currentWalPartition}");
            
            _logger.LogInformation($"Reading state file {stateFPath}");
            var savedState = JsonSerializer.Deserialize<TState>(File.ReadAllText(stateFPath!), JsonSerializerOptions);
            _state.Initialize(savedState);
            _inputDisruptor.PublishMessage(new StateReadFromFileEvt(), _currentWalPartition, true);
            
            _logger.LogInformation($"Reading wal file {walFPath}");
            var reader = new Reader(_transportMessagesAssemblies.ToArray());
            long i = _inputDisruptor.BufferSize;
            foreach (var message in reader.Read(walFPath!))
            {
                i--;
                if (i == 0) throw new InvalidOperationException("Too many messages in the wal file, increase the input disruptor buffer size");
                _inputDisruptor.PublishMessage(message.transportMessage, message.receivedAt, MetricsUtils.GetUnixMicro(), true, walPartition: _currentWalPartition);
            }
            
            _logger.LogInformation($"Completed reading wal file");
            _inputDisruptor.PublishMessage(new WalReadCompletedEvt(), _currentWalPartition, true);
        }
        else
        {
            // TODO
            throw new NotImplementedException();
        }
        
        _fs = File.Open(walFPath!, FileMode.Append,  FileAccess.Write, FileShare.Read);
        _logger.LogInformation("State manager started");
    }
    
    public void OnEvent(IncomingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        if (data.Skip) return;
        if (data.IsReplay) return;
        if (_stopRequested) return;

        if (data.ParsedMessage is StopEvt)
        {
            _stopRequested = true;
            Roll(sequence, _clock.AbsoluteClock.GetCurrentInstant().ToUnixTimeMilliseconds());
            data.WalPartition = _currentWalPartition;
            return;
        }

        var swStartProcessing = MetricsUtils.GetUnixMicro();
        
        _fs.Write(Encoding.UTF8.GetBytes(data.ReceivedAt.ToString()));
        _fs.Write(_delimiter);
        
        var transport = data.TransportMessage;
        _fs.Write(Encoding.UTF8.GetBytes(transport.GetType().FullName!));
        _fs.Write(_delimiter);
        _fs.Write(Encoding.UTF8.GetBytes(data.TransportMessage.SenderCompId));
        _fs.Write(_delimiter);
        _fs.Write(Encoding.UTF8.GetBytes(data.TransportMessage.MessageType.ToString()));
        _fs.Write(_delimiter);
        _fs.Write(Encoding.UTF8.GetBytes(data.TransportMessage.SessionId.ToString()));
        _fs.Write(_delimiter);
        _fs.Write(Encoding.UTF8.GetBytes(data.TransportMessage.SequenceNumber.ToString()));
        _fs.Write(_delimiter);
        if (!string.IsNullOrEmpty(data.TransportMessage.Payload)) _fs.Write(Encoding.UTF8.GetBytes(data.TransportMessage.Payload));
        // var payload = data.TransportMessage.Payload;
        // _fs.Write(Encoding.UTF8.GetBytes(payload.Substring(1, payload.Length - 2))); // Remove leading and trailing " placed by ZeroMq
        _fs.Write(_newLine);
        
        _fs.Flush();
        
        var rolled = false;
        if (_walRollPeriodSeconds != 0)
        {
            var currentTs = data.ReceivedAt;
            if (currentTs - _currentWalPartition >= _walRollPeriodSeconds)
            {
                _logger.LogInformation($"Rolling partition due to roll period");
                Roll(sequence, currentTs);
                rolled = true;
            }
        }

        if (!rolled && WalRollPeriodEvents != 0 && sequence - _currentWalPartitionStartSequence > WalRollPeriodEvents)
        {
            _logger.LogInformation($"Rolling partition due to number of events");
            Roll(sequence, data.ReceivedAt);
        }
        
        data.WalPartition = _currentWalPartition;
        
        _walWaitTime?.Observe(swStartProcessing - data.SwReceivedAt);
        _walTime?.Observe(MetricsUtils.GetUnixMicro() - swStartProcessing);
    }

    public string PersistState(TState state, long partition, bool createWalFile = false)
    {
        _logger.LogInformation($"Persisting state with partition {partition}");
        var stateFPath = Path.Combine(_config.WorkingDirPath, $"{partition}.state");
        var serialized = JsonSerializer.Serialize(state, JsonSerializerOptions);
        using var stateFile = File.OpenWrite(stateFPath);
        stateFile.Write(Encoding.UTF8.GetBytes(serialized));
        // JsonSerializer.Serialize(stateFile, state, JsonSerializerOptions);
        stateFile.Flush();
        _logger.LogInformation($"State persisted to {stateFPath}");

        if (createWalFile)
        {
            var walFPath = Path.Combine(_config.WorkingDirPath, $"{partition}.wal");
            using var walFile = File.Create(walFPath);
            _logger.LogInformation($"Created wal file {walFPath}");
        }

        return serialized;
    }
    
    public long CurrentWalPartition => _currentWalPartition;
    
    public void FinishReconciliation() => 
        _inputDisruptor.PublishMessage(new ReconciliationDoneEvt(), _currentWalPartition, true);

    private static (long?, string?) ScanFiles(string path, string extension)
    {
        extension = $".{extension}";
        var res = Directory
            .GetFiles(path)
            .Select(fpath =>
            {
                var ext = Path.GetExtension(fpath);
                if (string.IsNullOrEmpty(ext)) return null;
                if (ext != extension) return null;
                var fname = Path.GetFileNameWithoutExtension(fpath);
                if (!long.TryParse(fname, out var ts)) return null;
                return new { ts, fpath };
            })
            .Where(i => i != null)
            .OrderByDescending(i => i.ts)
            .FirstOrDefault();
        
        return (res?.ts, res?.fpath);
    }

    private void Roll(long sequence, long partition)
    {
        if (partition == _currentWalPartition) partition++;
        
        _logger.LogInformation($"Moving to new partition {partition}");
        
        _fs.Flush();
        _fs.Close();
        _fs.Dispose();
        
        _logger.LogInformation($"Current wal file finished");
        
        _currentWalPartition = partition;
        var newFilePath = Path.Combine(_config.WorkingDirPath, $"{partition}.wal");
        _fs = new FileStream(newFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
        _currentWalPartitionStartSequence = sequence;
        
        
        _logger.LogInformation($"Created new wal file {newFilePath}");
    }
    
    public void Dispose()
    {
        _fs.Flush();
        _fs.Close();
        _fs.Dispose();
    }
    
    private static Lazy<JsonSerializerOptions> _jsonSerializerOptions = new Lazy<JsonSerializerOptions>(() =>
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };
        options.ConfigureForNodaTime(new NodaJsonSettings(DateTimeZoneProviders.Tzdb));
        return options;
    });
    private static JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptions.Value;
}

public class WalDirectoryEmptyException : Exception
{
}

public class WalConfig
{
    public string WorkingDirPath { get; set; } = ".";
    public string? PersistStatePeriod { get; set; }
    public long MaxNumberOfEventsInWal { get; set; } = 1024;
    public bool TryRetrieveStateFromPersistentStorage { get; set; }
    
    public string ServiceName { get; set; }
    public bool Monolith { get; set; }
    public bool WritePerformanceMetrics { get; set; }
    public int[] WalWaitTimeParams { get; set; } = [20, 20, 10];
    public int[] WalTimeParams { get; set; } = [20, 20, 10];
}
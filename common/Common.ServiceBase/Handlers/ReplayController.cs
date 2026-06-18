using System.Buffers.Binary;
using Disruptor;
using NodaTime;
using NodaTime.Text;

namespace QuantInfra.Common.ServiceBase.Handlers;

public class ReplayController : IEventHandler<OutgoingDisruptorMessage>
{
    private readonly bool _finalizeEveryMessage;
    private readonly long? _timeCutoff;
    private readonly long? _msgCountCutoff;
    private readonly IClock _clock = SystemClock.Instance;
    private readonly string _filePath;

    private long _lastCutoffTime;
    private long _lastCutoffSeqNum;
    
    public ReplayController(FinalizerConfig config)
    {
        if (!string.IsNullOrEmpty(config.PeriodCutoff))
        {
            _timeCutoff = (long)PeriodPattern.Roundtrip.Parse(config.PeriodCutoff).Value.ToDuration().TotalMilliseconds;
        }

        _msgCountCutoff = config.NumberCutoff;
        
        _finalizeEveryMessage = !config.NumberCutoff.HasValue && string.IsNullOrEmpty(config.PeriodCutoff);

        _filePath = config.FilePath;
    }

    public long GetLastProcessedSequence()
    {
        try
        {
            var data = File.ReadAllBytes(_filePath);
            return BinaryPrimitives.ReadInt64LittleEndian(data);
        }
        catch (FileNotFoundException)
        {
            return 0;
        }
    }
    
    public void OnEvent(OutgoingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        var finalize = _finalizeEveryMessage;
        
        if (!finalize && _msgCountCutoff.HasValue)
        {
            finalize = sequence >= _lastCutoffSeqNum + _msgCountCutoff;
        }

        long now = 0;
        if (!finalize && _timeCutoff.HasValue)
        {
            now = _clock.GetCurrentInstant().ToUnixTimeMilliseconds();
            finalize = now - _lastCutoffTime > _timeCutoff;
        }

        if (finalize)
        {
            using var fs = new FileStream(
                _filePath,
                FileMode.Create,          // ← truncates or creates
                FileAccess.Write,
                FileShare.None,
                bufferSize: sizeof(long),
                options: FileOptions.SequentialScan);

            var buffer = new byte[4];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, sequence);
            fs.Write(buffer);
            fs.Flush();
        }

        _lastCutoffSeqNum = sequence;
        _lastCutoffTime = now;
    }
}

public class FinalizerConfig
{
    public long? NumberCutoff { get; set; }
    public string? PeriodCutoff { get; set; }
    public string FilePath { get; set; } = "seq.log";
}
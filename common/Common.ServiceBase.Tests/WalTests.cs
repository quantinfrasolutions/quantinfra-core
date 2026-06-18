using System.Reflection;
using System.Runtime.CompilerServices;
using Common.Utils.Reflection;
using Disruptor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using Disruptor.Dsl;
using QuantInfra.Common.Messaging;
using QuantInfra.Common.Messaging.Json;
using QuantInfra.Common.ServiceBase;
using QuantInfra.Common.ServiceBase.BPL;
using QuantInfra.Common.ServiceBase.WAL;
using QuantInfra.Tests.Mocks;

[assembly:InternalsVisibleTo("System.Text.Json")]

namespace Common.ServiceBase.Tests;

[TestOf(typeof(WalManager<MockState>))]
[TestOf(typeof(BusinessLogicProcessorBase<MockState>))]
public class WalTests : IExceptionHandler<IncomingDisruptorMessage>
{
#pragma warning disable NUnit1032
    private ServiceProvider _sp;
    private Disruptor<IncomingDisruptorMessage> _disruptor;
    private WalManager<MockState> _wal;
    private readonly FileClock _clock = new();
    private MockState _state;
#pragma warning restore NUnit1032

    internal Dictionary<long, SemaphoreSlim> Semaphores { get; } = new();
    public SemaphoreSlim? OnBeforeReplayingWalSemaphore { get; set; }
    public SemaphoreSlim? OnStateInitializedSemaphore { get; set; }

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        if (Directory.Exists("./data")) Directory.Delete("./data", true);
    }
    
    public void Setup()
    {
        if (_wal != null) _wal.Dispose();
        
        _sp = new ServiceCollection()
            .AddLogging(conf =>
            {
                conf.AddConsole();
            })
            .AddSingleton<WalConfig>(sp => new()
            {
                MaxNumberOfEventsInWal = 5,
                WorkingDirPath = "./data",
            })
            .AddSingleton<DisruptorConfig>(sp => new())
            .AddSingleton<MockState>(sp => new())
            .AddSingleton<List<Assembly>>(sp => new())
            .AddSingleton<ReplayingClock>(new ReplayingClock(_clock))
            .AddSingleton<IClock>(sp => sp.GetRequiredService<ReplayingClock>())
            // .AddSingleton<DownstreamFilter>()
            // .AddSingleton<FinalizerConfig>(sp => new() { EventIdCutoff = 100000 })
            .AddJsonMessages()
            .AddDefaultJsonSerializerSettings()
            // .AddSingleton<Finalizer>()
            .AddSingleton<SimpleBpl>()
            .AddSingleton<ITypeResolver>(sp => new SingleAssemblyTypeResolver(Assembly.GetExecutingAssembly()))
            .AddInputDisruptor()
            .AddOutputDisruptor()
            .AddWalManager<MockState>()
            .AddSingleton<WalTests>(sp => this)
            .BuildServiceProvider();
        
        _disruptor = _sp.GetRequiredService<Disruptor<IncomingDisruptorMessage>>();
        _wal = _sp.GetRequiredService<WalManager<MockState>>();
        var bpl = _sp.GetRequiredService<SimpleBpl>();
        _disruptor.HandleEventsWith(_wal).Then(new MockParser()).Then(bpl);
        _disruptor.SetDefaultExceptionHandler(this);
        _state = _sp.GetRequiredService<MockState>();
    }

    [Test, Order(1)]
    public void TestCreateWalManager()
    {
        Setup();
        _clock.Instant = Instant.FromUtc(2026, 1, 21, 18, 0);
        var ts = _clock.Instant.ToUnixTimeMilliseconds();
        
        var walManager = _sp.GetRequiredService<WalManager<MockState>>();
        walManager.Start();
        
        var files = Directory.GetFiles("./data");
        Assert.That(files.Length, Is.EqualTo(2));
        Assert.That(files.SingleOrDefault(f => f.EndsWith(".wal")), Is.EqualTo($"./data/{ts}.wal"));
        Assert.That(files.SingleOrDefault(f => f.EndsWith(".state")), Is.EqualTo($"./data/{ts}.state"));
    }

    [Test, Order(2)]
    public async Task TestReadExistingEmptyFilesAndProcessMessage()
    {
        Setup();
        _wal.Start();
        OnStateInitializedSemaphore = new(0);
        _disruptor.Start();

        await OnStateInitializedSemaphore.WaitAsync();
        OnStateInitializedSemaphore = null;

        CreateSemaphore(1);
        SendMessage(1, "1");
        await WaitForSemaphore(1);
        
        ValidateState(1, "1");
        CreateSemaphore(2);
        SendMessage(2, "2");
        await WaitForSemaphore(2);
        ValidateState(2, "2");
    }

    [Test, Order(3)]
    public async Task TestReadNonEmptyWalFile()
    {
        Setup();
        _wal.Start();
        OnStateInitializedSemaphore = new(0);
        _disruptor.Start();
        await OnStateInitializedSemaphore.WaitAsync();
        OnStateInitializedSemaphore = null;
        
        ValidateState(2, "2");
    }

    [Test, Order(4)]
    public async Task TestRollBySequenceNumber()
    {
        _clock.Instant = _clock.Instant.Plus(Duration.FromSeconds(1));
        
        for (var i = 3; i < 5; i++)
        {
            CreateSemaphore(i);
            SendMessage(i, i.ToString());
            await WaitForSemaphore(i);
            ValidateState(i, i.ToString());
            
            Assert.That(Directory.GetFiles("./data").Length, Is.EqualTo(2));
        }
        
        CreateSemaphore(5);
        
        SendMessage(5, "5");
        await WaitForSemaphore(5);
        ValidateState(5, "5");
        
        await Task.Delay(100);
        var files = Directory.GetFiles("./data");
        Assert.That(files.Length, Is.EqualTo(4));
        var ts = _clock.Instant.ToUnixTimeMilliseconds();
        Assert.That(files.SingleOrDefault(f => f.EndsWith($"{ts}.wal")), Is.EqualTo($"./data/{ts}.wal"));
        Assert.That(files.SingleOrDefault(f => f.EndsWith($"{ts}.state")), Is.EqualTo($"./data/{ts}.state"));
    }

    [Test, Order(5)]
    public async Task TestReadNonEmptyState()
    {
        Setup();
        _wal.Start();
        OnStateInitializedSemaphore = new(0);
        _disruptor.Start();
        await OnStateInitializedSemaphore.WaitAsync();
        OnStateInitializedSemaphore = null;
        
        ValidateState(5, "5");
    }

    [Test, Order(6)]
    public async Task TestSimultaneousMessages()
    {
        CreateSemaphore(18);
        for (var i = 6; i <= 18; i++)
        {
            _clock.Instant += Duration.FromSeconds(1);
            SendMessage(i, i.ToString());
        }
        
        await WaitForSemaphore(18);
        ValidateState(18, "18");
    }

    private void CreateSemaphore(int seqNum)
    {
        Semaphores[seqNum] = new SemaphoreSlim(0);
    }
    private async Task WaitForSemaphore(int seqNum)
    {
        await Semaphores[seqNum].WaitAsync();
        Semaphores.Remove(seqNum);
    }

    private void SendMessage(long seqNum, string payload) =>
        _disruptor.PublishMessage(
            new MockMessage("1", MessageType.DataMessage, 1, seqNum, payload),
            _clock.GetCurrentInstant().ToUnixTimeMilliseconds(), _clock.GetCurrentInstant().ToUnixTimeMilliseconds()
        );

    private void ValidateState(int seqNum, string payload)
    {
        Assert.That(_state.LastEventId, Is.EqualTo(payload));
        Assert.That(_state.EventIds.Count, Is.EqualTo(seqNum));
    }

    private int _eventExceptionsCount = 0;
    public void HandleEventException(Exception ex, long sequence, IncomingDisruptorMessage evt)
    {
        _eventExceptionsCount++;
    }

    public void HandleOnTimeoutException(Exception ex, long sequence)
    {
        throw new NotImplementedException();
    }

    public void HandleEventException(Exception ex, long sequence, EventBatch<IncomingDisruptorMessage> batch)
    {
        throw new NotImplementedException();
    }

    public void HandleOnStartException(Exception ex)
    {
        throw new NotImplementedException();
    }

    public void HandleOnShutdownException(Exception ex)
    {
        throw new NotImplementedException();
    }
}

class MockState : IState<MockState>
{
    public string LastEventId { get; set; } = "";
    public List<string> EventIds { get; set; } = new();
    
    public void Initialize(MockState state)
    {
        LastEventId = state.LastEventId;
        EventIds = state.EventIds.ToList();
    }

    public long LastFinalizedEventId { get; }
    public long LastFinalizedTimestamp { get; }
    public void UpdateLastSentEventId(long eventId, long timestamp)
    {
        throw new NotImplementedException();
    }
}

class MockMessage : ITransportMessage
{
    public MockMessage() { }
    public MockMessage(string senderCompId, MessageType messageType, long sessionId, long sequenceNumber, string payload)
    {
        SenderCompId = senderCompId;
        MessageType = messageType;
        SessionId = sessionId;
        SequenceNumber = sequenceNumber;
        Payload = payload;
    }

    public string SenderCompId { get; private set; }
    public MessageType MessageType { get; private set; }
    public long SessionId { get; private set; }
    public long SequenceNumber { get; private set; }
    public long SendingTimestamp { get; }
    public string Payload { get; private set; }

    public void ConstructFromLog(string senderCompId, MessageType messageType, long sessionId, long sequenceNumber,
        string payload)
    {
        SenderCompId = senderCompId;
        MessageType = messageType;
        SessionId = sessionId;
        SequenceNumber = sequenceNumber;
        Payload = payload;
    }
}

class MockParser : IEventHandler<IncomingDisruptorMessage>
{
    public void OnEvent(IncomingDisruptorMessage data, long sequence, bool endOfBatch)
    {
        if (data.TransportMessage is { MessageType: MessageType.DataMessage }) 
            data.SetParsedMessage(data.TransportMessage.Payload);
    }
}
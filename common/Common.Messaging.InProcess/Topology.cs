using Disruptor;
using Disruptor.Dsl;
using QuantInfra.Common.Messaging.InProcess.Messages.TopicMulticast;
using QuantInfra.Common.Messaging.Patterns.DealerRouterWithReplay;
using QuantInfra.Common.Messaging.Patterns.TopicMulticast;
using QuantInfra.Common.ServiceBase;

namespace QuantInfra.Common.Messaging.InProcess;

public class TopologyConfig
{
    public int DisruptorRingBufferSize { get; set; } = 1024;
    public WaitStrategy WaitStrategy { get; set; } = WaitStrategy.BlockingWait;
    
    public IWaitStrategy GetWaitStrategy() => DisruptorConfig.GetWaitStrategy(WaitStrategy);
}

public class DisruptorMessage
{
    public MsgType? MessageType { get; set; }
    public string? ServerName { get; set; }
    public string? TopicName { get; set; }
    public ITransportMessage? Message { get; set; }
    public IListener? Listener { get; set; }
    public RequestSnapshotMessage? RequestSnapshot { get; set; }
    public MulticastTransport? MulticastTransport { get; set; }
}

public enum MsgType
{
    SubscribeToTopic,
    PublishToTopic,
    RegisterRouter,
    SendToRouter,
    RequestSnapshot,
    RegisterMulticastControlHandler
}

public class Topology : IEventHandler<DisruptorMessage>
{
    // private readonly Dictionary<string, List<IListener>> _multicastListeners = new();
    private readonly Node _root = new();
    private readonly Dictionary<string, IListener> _routers = new();
    private readonly Disruptor<DisruptorMessage> _disruptor;
    private readonly Dictionary<string, MulticastTransport> _multicastControlHandlers = new();


    public Topology(TopologyConfig config)
    {
        _disruptor = new Disruptor<DisruptorMessage>(
            () => new(),
            config.DisruptorRingBufferSize,
            config.GetWaitStrategy());
        _disruptor.HandleEventsWith(this);
        _disruptor.Start();
    }
    
    
    public void SubscribeToTopic(string topic, IListener listener)
    {
        using var scope = _disruptor.PublishEvent();
        var data = scope.Event();
        data.MessageType = MsgType.SubscribeToTopic;
        data.TopicName = topic;
        data.Listener = listener;
    }

    public void SendTopicMulticastMessage(string topicName, ITransportMessage msg)
    {
        using var scope = _disruptor.PublishEvent();
        var data = scope.Event();
        data.MessageType = MsgType.PublishToTopic;
        data.TopicName = topicName;
        data.Message = msg;
    }

    public void SendRequestSnapshotMessage(RequestSnapshotMessage message, string serverName)
    {
        using var scope = _disruptor.PublishEvent();
        var data = scope.Event();
        data.MessageType = MsgType.RequestSnapshot;
        data.ServerName = serverName;
        data.RequestSnapshot = message;
    }

    public void RegisterMulticastControlHandler(string serverName, MulticastTransport multicastTransport)
    {
        using var scope = _disruptor.PublishEvent();
        var data = scope.Event();
        data.MessageType = MsgType.RegisterMulticastControlHandler;
        data.ServerName = serverName;
        data.MulticastTransport = multicastTransport;
    }
    
    public void RegisterDealer(string serverName, string clientName, IListener listener)
    {
        throw new NotImplementedException();
    }
    
    public void SendControlMessageToDealer(string serverName, string clientId, ControlMessage message)
    {
        throw new NotImplementedException();
    }

    public void RegisterRouter(string serverName, IListener listener)
    {
        using var scope = _disruptor.PublishEvent();
        var data = scope.Event();
        data.MessageType = MsgType.RegisterRouter;
        data.Listener = listener;
        data.ServerName = serverName;
    }
    
    public void SendMessageToRouter(string serverName, ITransportMessage msg)
    {
        using var scope = _disruptor.PublishEvent();
        var data = scope.Event();
        data.MessageType = MsgType.SendToRouter;
        data.ServerName = serverName;
        data.Message = msg;
    }
    

    public void OnEvent(DisruptorMessage data, long sequence, bool endOfBatch)
    {
        switch (data.MessageType)
        {
            case MsgType.SubscribeToTopic:
                var topic = data.TopicName!;
                
                var parts = topic.Split('.', StringSplitOptions.RemoveEmptyEntries);
                var node = _root;
                foreach (var part in parts)
                {
                    if (!node.Children.TryGetValue(part, out var child))
                    {
                        child = new Node();
                        node.Children[part] = child;
                    }
                    node = child;
                }
                node.Subscribers.Add(data.Listener!);
                
                break;
            
            case MsgType.PublishToTopic:
                var topicName = data.TopicName!;
                var topicParts = topicName.Split('.', StringSplitOptions.RemoveEmptyEntries);
                var result = new List<IListener>();
                MatchRecursive(_root, topicParts, 0, result);
                foreach (var listener in result) listener.ReceiveMessage(data.Message!, topicName: data.TopicName);
                break;
            
            case MsgType.RegisterRouter:
                _routers.Add(data.ServerName!, data.Listener!);
                break;
            
            case MsgType.SendToRouter:
                _routers[data.ServerName!].ReceiveMessage(data.Message!);
                break;
            
            case MsgType.RequestSnapshot:
                if (_multicastControlHandlers.TryGetValue(data.ServerName!, out var multicast))
                    multicast.ReceiveRequestSnapshotMessage(data.RequestSnapshot!);
                break;
            
            case MsgType.RegisterMulticastControlHandler:
                _multicastControlHandlers.Add(data.ServerName!, data.MulticastTransport!);
                break;
            
            default:
                throw new NotSupportedException($"Unhandled message type {data.MessageType}");
        }
    }
    
    private static void MatchRecursive(
        Node node,
        string[] parts,
        int index,
        List<IListener> result
    )
    {
        result.AddRange(node.Subscribers);
        
        if (index == parts.Length) return;

        var part = parts[index];

        // Exact match: a.b.1
        if (node.Children.TryGetValue(part, out var exactChild))
        {
            MatchRecursive(exactChild, parts, index + 1, result);
        }

        // Wildcard match: a.b.*
        if (node.Children.TryGetValue("*", out var wildcardChild))
        {
            MatchRecursive(wildcardChild, parts, index + 1, result);
        }
    }
    
    private sealed class Node
    {
        public Dictionary<string, Node> Children { get; } = new();
        public List<IListener> Subscribers { get; } = new();
    }
}
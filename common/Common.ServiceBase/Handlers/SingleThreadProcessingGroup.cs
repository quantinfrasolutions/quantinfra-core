using Disruptor;
using Disruptor.Dsl;

namespace QuantInfra.Common.ServiceBase.Handlers;

public class SingleThreadProcessingGroup<TMessage>(params IEventHandler<TMessage>[] handlers) : IEventHandler<TMessage>
{
    public void OnEvent(TMessage data, long sequence, bool endOfBatch)
    {
        foreach (var handler in handlers)
        {
            handler.OnEvent(data, sequence, endOfBatch);
        }
    }
}

public static class SingleThreadProcessingGroupExtensions
{
    public static void ConfigureDisruptor<TMessage>(this Disruptor<TMessage> disruptor, params GroupConfig<TMessage>[] processors)
        where TMessage : class
    {
        if (processors.Length == 0) return;
        
        GroupConfig<TMessage> processorConfig = processors[0];
        if (processors.Length == 1)
        {
            disruptor.HandleEventsWith(processorConfig.Handler);
            return;
        }
        
        var groups = processors.GroupBy(x => x.Group)
            .OrderBy(x => x.Key)
            .Select(gr => gr.Count() == 1 
                ? gr.First().Handler 
                : new SingleThreadProcessingGroup<TMessage>(gr.Select(x => x.Handler).ToArray())
            ).ToArray();

        EventHandlerGroup<TMessage> group = null!;
        for (var i = 0; i < groups.Length; i++)
        {
            if (i == 0) group = disruptor.HandleEventsWith(groups[i]);
            else group = group.Then(groups[i]);
        }
    }
}

public record GroupConfig<TMessage>(IEventHandler<TMessage> Handler, int Group);
namespace QuantInfra.Common.EventSourcing;

public class PropagatorOptions
{
    public bool PropagateEvents { get; set; } = false;
    public string PropagationPublisherName { get; set; } = default!;
}
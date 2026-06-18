using System;

namespace QuantInfra.Common.Messaging;

public class MockMessage
{
    public string Sender { get; set; }
    public long Sequence { get; set; }
    public int Value { get; set; }

    protected bool Equals(MockMessage other)
    {
        return Sender == other.Sender && Sequence == other.Sequence && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((MockMessage)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Sender, Sequence, Value);
    }
}
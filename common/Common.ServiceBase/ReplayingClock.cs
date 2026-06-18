using NodaTime;

namespace QuantInfra.Common.ServiceBase;

public class ReplayingClock(IClock absoluteClock) : IClock
{
    private Instant _currentInstant;
    private bool _replayFinished;
    public IClock AbsoluteClock => absoluteClock;

    public void SetCurrentInstant(long receivedAt) => _currentInstant = Instant.FromUnixTimeMilliseconds(receivedAt);
    public Instant GetCurrentInstant() => _replayFinished ? absoluteClock.GetCurrentInstant() : _currentInstant;
    
    public void FinishReplay() => _replayFinished = true;
}
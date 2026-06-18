using NodaTime;

namespace StrategiesCore
{
    public class StrategiesServiceState
    {
        public Instant? LastProcessedEvtDt { get; set; }
    }
}
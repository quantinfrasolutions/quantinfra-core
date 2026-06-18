using NodaTime;

namespace QuantInfra.Tests.Mocks
{
	public class FileClock : IClock
	{
        public FileClock()
		{
			Instant = InitialValue;
		}

		public Instant GetCurrentInstant() => Instant;

		public readonly Instant InitialValue = Instant
            .FromUtc(2018, 03, 01, 00, 00, 00)
            .Minus(Duration.FromSeconds(1));
        public Instant Instant { get; set; }

        public void AddMinutes(int minutes)
        {
	        Instant = Instant.Plus(Duration.FromMinutes(minutes));
        }

        public void AddHours(int hours)
        {
	        Instant = Instant.Plus(Duration.FromHours(hours));
        }

        public void AddSeconds(int seconds)
        {
	        Instant = Instant.Plus(Duration.FromSeconds(seconds));
        }
	}
}


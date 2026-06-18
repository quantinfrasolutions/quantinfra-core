using System;
namespace Databases.MarketDataHistory
{
	public class Config
	{
		public string Host { get; set; } = "localhost";
		public int Port { get; set; } = 5432;
		public string User { get; set; } = "postgres";
		public string Password { get; set; } = "password";
		public string Database { get; set; } = "market_data";
		public bool IncludeErrorDetail { get; set; } = false;
		public string ConnectionStringExtras { get; set; } = default!;
		public bool EnableLowLevelLogging { get; set; } = false;
		public int MaxPoolSize { get; set; } = 100;
		public int ConnectionTimeoutSec { get; set; } = 15;
		public int CommandTimeoutSec { get; set; } = 30;
    }
}

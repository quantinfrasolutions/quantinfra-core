namespace QuantInfra.Databases.Main;

public class Config
{
	public string Host { get; set; } = "localhost";
	public int Port { get; set; } = 5432;
	public string User { get; set; } = "postgres";
	public string Password { get; set; } = "password";
	public string Database { get; set; } = "main";
	public bool IncludeErrorDetail { get; set; } = false;
	public string ConnectionStringExtras { get; set; } = default!;
	public bool EnableLowLevelLogging { get; set; } = false;
	public int MinPoolSize { get; set; } = 0;
	public int MaxPoolSize { get; set; } = 100;
    public int ConnectionTimeoutSec { get; set; } = 15;
    public int CommandTimeoutSec { get; set; } = 30;
}

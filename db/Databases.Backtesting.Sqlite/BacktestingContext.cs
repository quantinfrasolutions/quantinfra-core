using Microsoft.EntityFrameworkCore;
using QuantInfra.Common.Interfaces.Api.Backtesting;

namespace QuantInfra.Databases.Backtesting.Sqlite;

public class BacktestingContext : DbContext
{
    private readonly Config? _config;

    public BacktestingContext(Config? config = null)
    {
        _config = config;
    }
    
    public DbSet<TestUnitStatusRecord> TestUnits { get; init; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var config = _config ?? new();
        ConfigurationExtensions.ConfigureOptions(optionsBuilder, config);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestUnitStatusRecord>(Models.TestUnitStatusMapping.Configure);
    }
}
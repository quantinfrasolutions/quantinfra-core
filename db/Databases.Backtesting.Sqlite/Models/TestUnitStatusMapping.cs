using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using QuantInfra.Common.Interfaces.Api.Backtesting;
using QuantInfra.Sdk.Backtesting;
using QuantInfra.Sdk.StaticData;

namespace QuantInfra.Databases.Backtesting.Sqlite.Models;

public class TestUnitStatusMapping
{
    public static void Configure(EntityTypeBuilder<TestUnitStatusRecord> builder)
    {
        var jsonOptions = new JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
            PropertyNameCaseInsensitive = true,
        };
        jsonOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        jsonOptions.Converters.Add(new JsonStringEnumConverter());

        builder.ToTable("test_units");
        builder.HasKey(s => s.TestId);
        builder.Property(s => s.TestId).HasColumnName("test_id").IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(s => s.Action).HasColumnName("action").IsRequired();
        builder.Property(s => s.MetricsCalculatorName).HasColumnName("metrics_calculator_name");
        builder.OwnsOne(s => s.Options, o =>
        {
            o.Property(x => x.StartDt).HasColumnName("start_dt").IsRequired();
            o.Property(x => x.EndDt).HasColumnName("end_dt").IsRequired();
            o.Property(s => s.LogLevel).HasColumnName("log_level");
            o.Property(x => x.Investment).HasColumnName("investment").IsRequired();
            o.Property(x => x.CandlesTimeframe).HasColumnName("candles_timeframe").IsRequired()
                .HasConversion(new PeriodConverter());
            o.Property(x => x.MtmUtcOffset).HasColumnName("mtm_utc_offset").IsRequired()
                .HasConversion(new DurationConverter());
            o.Property(x => x.RequestBarAttempts).HasColumnName("request_bar_attempts").IsRequired();
            o.Property(x => x.ThrowOnZeroVolumeOrders).HasColumnName("throw_on_zero_volume_orders").IsRequired();
            o.Property(x => x.VirtualAccountSizeStepFraction).HasColumnName("virtual_account_size_step_fraction").IsRequired();
            o.Property(x => x.CheckPendingOrdersExecutionUsingHighLow).HasColumnName("check_pending_orders_execution_using_high_low").IsRequired();
            o.Property(x => x.CheckOrdersAtBarOpen).HasColumnName("check_orders_at_bar_open").IsRequired();
            o.Property(x => x.CheckOrdersAtBarClose).HasColumnName("check_orders_at_bar_close").IsRequired();
            o.Property(x => x.LimitCloseCheckToMarketOrdersOnly).HasColumnName("limit_close_check_to_market_orders_only").IsRequired();
            o.Property(x => x.StopOrdersExecution).HasColumnName("stop_orders_execution").IsRequired();
            o.Property(x => x.OpenExecutionOffset).HasColumnName("open_execution_offset").IsRequired()
                .HasConversion(new DurationConverter());
            o.Property(x => x.HighLowExecutionOffset).HasColumnName("high_low_execution_offset").IsRequired()
                .HasConversion(new DurationConverter());
        });
        builder.OwnsOne(s => s.PersistOptions, o =>
        {
            o.Property(x => x.SaveStrategies).HasColumnName("save_strategies").IsRequired();
            o.Property(x => x.SaveTrades).HasColumnName("save_trades").IsRequired();
            o.Property(x => x.ExpectedNumberOfTradesPerDay).HasColumnName("expected_trades_per_day").IsRequired();
            o.Property(x => x.SavePositions).HasColumnName("save_positions").IsRequired();
            o.Property(x => x.SaveDailyReturns).HasColumnName("save_daily_returns").IsRequired();
            o.Property(x => x.DoNotSaveZeroDailyReturns).HasColumnName("skip_zero_daily_returns").IsRequired();
            o.Property(x => x.SaveEndOfDayValues).HasColumnName("save_end_of_day_values").IsRequired();
            o.Property(x => x.ExpectedNumberOfOpenPositionsAtEndOfDay).HasColumnName("expected_positions_per_day").IsRequired();
            o.Property(x => x.SaveStrategies).HasColumnName("save_strategies").IsRequired();
            o.Property(x => x.SaveMetrics).HasColumnName("save_metrics").IsRequired();
        });
        builder.Property(s => s.ContractOverride).HasColumnName("contract_override").HasColumnType("string")
            .HasConversion(new JsonValueConverter<ContractOverride?>());
        builder.Property(s => s.Data).HasColumnName("data");
        builder.Property(s => s.MetricsCalculorData).HasColumnName("metrics_calculator_data");
        builder.Property(s => s.Status).HasColumnName("status").HasColumnType("text").IsRequired();
        builder.Property(s => s.StatusMessage).HasColumnName("status_message");
    }
}
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuantInfra.Databases.Backtesting.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "test_units",
                columns: table => new
                {
                    test_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    status = table.Column<int>(type: "text", nullable: false),
                    status_message = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<string>(type: "TEXT", nullable: false),
                    action = table.Column<string>(type: "TEXT", nullable: false),
                    start_dt = table.Column<string>(type: "TEXT", nullable: false),
                    end_dt = table.Column<string>(type: "TEXT", nullable: false),
                    log_level = table.Column<int>(type: "INTEGER", nullable: false),
                    investment = table.Column<decimal>(type: "TEXT", nullable: false),
                    candles_timeframe = table.Column<string>(type: "TEXT", nullable: false),
                    mtm_utc_offset = table.Column<string>(type: "TEXT", nullable: false),
                    request_bar_attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    throw_on_zero_volume_orders = table.Column<bool>(type: "INTEGER", nullable: false),
                    virtual_account_size_step_fraction = table.Column<int>(type: "INTEGER", nullable: false),
                    days_in_year = table.Column<int>(type: "INTEGER", nullable: false),
                    check_pending_orders_execution_using_high_low = table.Column<bool>(type: "INTEGER", nullable: false),
                    check_orders_at_bar_open = table.Column<bool>(type: "INTEGER", nullable: false),
                    check_orders_at_bar_close = table.Column<bool>(type: "INTEGER", nullable: false),
                    limit_close_check_to_market_orders_only = table.Column<bool>(type: "INTEGER", nullable: false),
                    stop_orders_execution = table.Column<int>(type: "INTEGER", nullable: false),
                    open_execution_offset = table.Column<string>(type: "TEXT", nullable: false),
                    high_low_execution_offset = table.Column<string>(type: "TEXT", nullable: false),
                    record_share_prices = table.Column<bool>(type: "INTEGER", nullable: false),
                    record_trades = table.Column<bool>(type: "INTEGER", nullable: false),
                    expected_trades_per_day = table.Column<int>(type: "INTEGER", nullable: false),
                    record_position_closes = table.Column<bool>(type: "INTEGER", nullable: false),
                    record_end_of_day_positions = table.Column<bool>(type: "INTEGER", nullable: false),
                    expected_positions_per_day = table.Column<int>(type: "INTEGER", nullable: false),
                    save_strategies = table.Column<bool>(type: "INTEGER", nullable: false),
                    save_trades = table.Column<bool>(type: "INTEGER", nullable: false),
                    save_positions = table.Column<bool>(type: "INTEGER", nullable: false),
                    save_daily_returns = table.Column<bool>(type: "INTEGER", nullable: false),
                    skip_zero_daily_returns = table.Column<bool>(type: "INTEGER", nullable: false),
                    save_end_of_day_values = table.Column<bool>(type: "INTEGER", nullable: false),
                    save_metrics = table.Column<bool>(type: "INTEGER", nullable: false),
                    contract_override = table.Column<string>(type: "string", nullable: true),
                    data = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_test_units", x => x.test_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "test_units");
        }
    }
}

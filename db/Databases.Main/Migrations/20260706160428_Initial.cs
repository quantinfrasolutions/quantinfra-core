using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using QuantInfra.Sdk.MarketData;
using QuantInfra.Sdk.Strategies;
using QuantInfra.Sdk.Trading.Orders;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuantInfra.Databases.Main.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "entities");

            migrationBuilder.EnsureSchema(
                name: "infrastructure");

            migrationBuilder.EnsureSchema(
                name: "static_data");

            migrationBuilder.EnsureSchema(
                name: "history");

            migrationBuilder.EnsureSchema(
                name: "market_data");

            migrationBuilder.EnsureSchema(
                name: "events");

            migrationBuilder.EnsureSchema(
                name: "projections");

            migrationBuilder.CreateSequence<int>(
                name: "accounts_seq",
                schema: "entities",
                startValue: 1000000L);

            migrationBuilder.CreateSequence<int>(
                name: "assets_seq",
                schema: "static_data",
                startValue: 100000L);

            migrationBuilder.CreateSequence<int>(
                name: "brokers_seq",
                schema: "static_data",
                startValue: 102L);

            migrationBuilder.CreateSequence<int>(
                name: "commissions_seq",
                schema: "static_data",
                startValue: 1000L);

            migrationBuilder.CreateSequence<int>(
                name: "contract_templates_seq",
                schema: "static_data",
                startValue: 10000L);

            migrationBuilder.CreateSequence<int>(
                name: "contracts_seq",
                schema: "static_data",
                startValue: 100000L);

            migrationBuilder.CreateSequence<int>(
                name: "datafeeds_seq",
                schema: "static_data",
                startValue: 101L);

            migrationBuilder.CreateSequence<int>(
                name: "exchanges_seq",
                schema: "static_data",
                startValue: 120L);

            migrationBuilder.CreateSequence(
                name: "position_history_id_seq",
                schema: "history",
                startValue: 100000000L);

            migrationBuilder.CreateSequence<int>(
                name: "strategies_seq",
                schema: "entities",
                startValue: 50000L);

            migrationBuilder.CreateSequence<int>(
                name: "streams_seq",
                schema: "static_data",
                startValue: 1000000L);

            migrationBuilder.CreateSequence<int>(
                name: "subaccounts_seq",
                schema: "entities",
                startValue: 1000000L);

            migrationBuilder.CreateSequence<int>(
                name: "trading_sessions_seq",
                schema: "static_data",
                startValue: 1000L);

            migrationBuilder.CreateSequence<int>(
                name: "ts_intervals_seq",
                schema: "static_data",
                startValue: 10000L);

            migrationBuilder.CreateTable(
                name: "assets",
                schema: "static_data",
                columns: table => new
                {
                    asset_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('static_data.assets_seq')"),
                    type = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assets", x => x.asset_id);
                });

            migrationBuilder.CreateTable(
                name: "brokers",
                schema: "static_data",
                columns: table => new
                {
                    broker_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('static_data.brokers_seq')"),
                    name = table.Column<string>(type: "text", nullable: false),
                    broker_type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brokers", x => x.broker_id);
                });

            migrationBuilder.CreateTable(
                name: "datafeeds",
                schema: "static_data",
                columns: table => new
                {
                    datafeed_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('static_data.datafeeds_seq')"),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_datafeeds", x => x.datafeed_id);
                });

            migrationBuilder.CreateTable(
                name: "exchanges",
                schema: "static_data",
                columns: table => new
                {
                    exchange_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('static_data.exchanges_seq')"),
                    name = table.Column<string>(type: "text", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exchanges", x => x.exchange_id);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                schema: "infrastructure",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "currencies",
                schema: "static_data",
                columns: table => new
                {
                    currency_id = table.Column<int>(type: "integer", nullable: false),
                    decimals = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currencies", x => x.currency_id);
                    table.ForeignKey(
                        name: "FK_currencies_assets_currency_id",
                        column: x => x.currency_id,
                        principalSchema: "static_data",
                        principalTable: "assets",
                        principalColumn: "asset_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trading_sessions",
                schema: "static_data",
                columns: table => new
                {
                    trading_session_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('static_data.trading_sessions_seq')"),
                    name = table.Column<string>(type: "text", nullable: false),
                    exchange_id = table.Column<int>(type: "integer", nullable: false),
                    is_24x7 = table.Column<bool>(type: "boolean", nullable: false),
                    is_rth = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trading_sessions", x => x.trading_session_id);
                    table.ForeignKey(
                        name: "FK_trading_sessions_exchanges_exchange_id",
                        column: x => x.exchange_id,
                        principalSchema: "static_data",
                        principalTable: "exchanges",
                        principalColumn: "exchange_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "as_instances",
                schema: "infrastructure",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false),
                    location_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_as_instances", x => x.name);
                    table.ForeignKey(
                        name: "FK_as_instances_locations_location_name",
                        column: x => x.location_name,
                        principalSchema: "infrastructure",
                        principalTable: "locations",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "es_instances",
                schema: "infrastructure",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false),
                    location_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_es_instances", x => x.name);
                    table.ForeignKey(
                        name: "FK_es_instances_locations_location_name",
                        column: x => x.location_name,
                        principalSchema: "infrastructure",
                        principalTable: "locations",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "market_data_clients",
                schema: "infrastructure",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false),
                    location_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_data_clients", x => x.name);
                    table.ForeignKey(
                        name: "FK_market_data_clients_locations_location_name",
                        column: x => x.location_name,
                        principalSchema: "infrastructure",
                        principalTable: "locations",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ss_instances",
                schema: "infrastructure",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false),
                    location_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ss_instances", x => x.name);
                    table.ForeignKey(
                        name: "FK_ss_instances_locations_location_name",
                        column: x => x.location_name,
                        principalSchema: "infrastructure",
                        principalTable: "locations",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commissions",
                schema: "static_data",
                columns: table => new
                {
                    commission_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('static_data.commissions_seq')"),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    fixed_per_share = table.Column<decimal>(type: "numeric", nullable: false),
                    floating = table.Column<decimal>(type: "numeric", nullable: false),
                    currency_id = table.Column<int>(type: "integer", nullable: true),
                    commission_structure_type = table.Column<string>(type: "text", nullable: false),
                    broker_id = table.Column<int>(type: "integer", nullable: true),
                    exchange_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commissions", x => x.commission_id);
                    table.ForeignKey(
                        name: "FK_commissions_brokers_broker_id",
                        column: x => x.broker_id,
                        principalSchema: "static_data",
                        principalTable: "brokers",
                        principalColumn: "broker_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commissions_currencies_currency_id",
                        column: x => x.currency_id,
                        principalSchema: "static_data",
                        principalTable: "currencies",
                        principalColumn: "currency_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_commissions_exchanges_exchange_id",
                        column: x => x.exchange_id,
                        principalSchema: "static_data",
                        principalTable: "exchanges",
                        principalColumn: "exchange_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contract_templates",
                schema: "static_data",
                columns: table => new
                {
                    template_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('static_data.contract_templates_seq')"),
                    name = table.Column<string>(type: "text", nullable: false),
                    security_type = table.Column<string>(type: "text", nullable: false),
                    pl_calculator_type = table.Column<string>(type: "text", nullable: false),
                    asset_id = table.Column<int>(type: "integer", nullable: true),
                    min_size = table.Column<decimal>(type: "numeric", nullable: false),
                    min_size_money = table.Column<decimal>(type: "numeric", nullable: true),
                    max_size = table.Column<decimal>(type: "numeric", nullable: false),
                    max_size_money = table.Column<decimal>(type: "numeric", nullable: true),
                    size_increment = table.Column<decimal>(type: "numeric", nullable: false),
                    tick_size = table.Column<decimal>(type: "numeric", nullable: false),
                    tick_value = table.Column<decimal>(type: "numeric", nullable: false),
                    price_quotation = table.Column<decimal>(type: "numeric", nullable: false),
                    settlement_currency_id = table.Column<int>(type: "integer", nullable: false),
                    base_currency_id = table.Column<int>(type: "integer", nullable: true),
                    quote_currency_id = table.Column<int>(type: "integer", nullable: true),
                    default_datafeed_id = table.Column<int>(type: "integer", nullable: true),
                    exchange_id = table.Column<int>(type: "integer", nullable: false),
                    broker_id = table.Column<int>(type: "integer", nullable: false),
                    days_in_year = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_templates", x => x.template_id);
                    table.ForeignKey(
                        name: "FK_contract_templates_assets_asset_id",
                        column: x => x.asset_id,
                        principalSchema: "static_data",
                        principalTable: "assets",
                        principalColumn: "asset_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_templates_brokers_broker_id",
                        column: x => x.broker_id,
                        principalSchema: "static_data",
                        principalTable: "brokers",
                        principalColumn: "broker_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_templates_currencies_base_currency_id",
                        column: x => x.base_currency_id,
                        principalSchema: "static_data",
                        principalTable: "currencies",
                        principalColumn: "currency_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_templates_currencies_quote_currency_id",
                        column: x => x.quote_currency_id,
                        principalSchema: "static_data",
                        principalTable: "currencies",
                        principalColumn: "currency_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_templates_currencies_settlement_currency_id",
                        column: x => x.settlement_currency_id,
                        principalSchema: "static_data",
                        principalTable: "currencies",
                        principalColumn: "currency_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_templates_datafeeds_default_datafeed_id",
                        column: x => x.default_datafeed_id,
                        principalSchema: "static_data",
                        principalTable: "datafeeds",
                        principalColumn: "datafeed_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_templates_exchanges_exchange_id",
                        column: x => x.exchange_id,
                        principalSchema: "static_data",
                        principalTable: "exchanges",
                        principalColumn: "exchange_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "currency_overrides",
                schema: "static_data",
                columns: table => new
                {
                    currency_id = table.Column<int>(type: "integer", nullable: false),
                    broker_id = table.Column<int>(type: "integer", nullable: false),
                    decimals = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currency_overrides", x => new { x.currency_id, x.broker_id });
                    table.ForeignKey(
                        name: "FK_currency_overrides_brokers_broker_id",
                        column: x => x.broker_id,
                        principalSchema: "static_data",
                        principalTable: "brokers",
                        principalColumn: "broker_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_currency_overrides_currencies_currency_id",
                        column: x => x.currency_id,
                        principalSchema: "static_data",
                        principalTable: "currencies",
                        principalColumn: "currency_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trading_session_intervals",
                schema: "static_data",
                columns: table => new
                {
                    interval_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('static_data.ts_intervals_seq')"),
                    trading_session_id = table.Column<int>(type: "integer", nullable: false),
                    start_day = table.Column<int>(type: "integer", nullable: false),
                    start = table.Column<LocalTime>(type: "time", nullable: false),
                    end_day = table.Column<int>(type: "integer", nullable: false),
                    end = table.Column<LocalTime>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trading_session_intervals", x => x.interval_id);
                    table.ForeignKey(
                        name: "FK_trading_session_intervals_trading_sessions_trading_session_~",
                        column: x => x.trading_session_id,
                        principalSchema: "static_data",
                        principalTable: "trading_sessions",
                        principalColumn: "trading_session_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "entities",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('entities.accounts_seq')"),
                    account_service = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    currency_id = table.Column<int>(type: "integer", nullable: false),
                    account_type = table.Column<string>(type: "text", nullable: false),
                    position_accounting = table.Column<string>(type: "text", nullable: false),
                    broker_id = table.Column<int>(type: "integer", nullable: true),
                    enable_share_price_tracking = table.Column<bool>(type: "boolean", nullable: false),
                    include_unrealized_pnl_to_mtm = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.account_id);
                    table.ForeignKey(
                        name: "FK_accounts_as_instances_account_service",
                        column: x => x.account_service,
                        principalSchema: "infrastructure",
                        principalTable: "as_instances",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_accounts_brokers_broker_id",
                        column: x => x.broker_id,
                        principalSchema: "static_data",
                        principalTable: "brokers",
                        principalColumn: "broker_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_accounts_currencies_currency_id",
                        column: x => x.currency_id,
                        principalSchema: "static_data",
                        principalTable: "currencies",
                        principalColumn: "currency_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contract_templates_commissions",
                schema: "static_data",
                columns: table => new
                {
                    contract_template_id = table.Column<int>(type: "integer", nullable: false),
                    commission_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_templates_commissions", x => new { x.contract_template_id, x.commission_id });
                    table.ForeignKey(
                        name: "FK_contract_templates_commissions_commissions_commission_id",
                        column: x => x.commission_id,
                        principalSchema: "static_data",
                        principalTable: "commissions",
                        principalColumn: "commission_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contract_templates_commissions_contract_templates_contract_~",
                        column: x => x.contract_template_id,
                        principalSchema: "static_data",
                        principalTable: "contract_templates",
                        principalColumn: "template_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_templates_trading_sessions",
                schema: "static_data",
                columns: table => new
                {
                    contract_template_id = table.Column<int>(type: "integer", nullable: false),
                    trading_session_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_templates_trading_sessions", x => new { x.contract_template_id, x.trading_session_id });
                    table.ForeignKey(
                        name: "FK_contract_templates_trading_sessions_contract_templates_cont~",
                        column: x => x.contract_template_id,
                        principalSchema: "static_data",
                        principalTable: "contract_templates",
                        principalColumn: "template_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contract_templates_trading_sessions_trading_sessions_tradin~",
                        column: x => x.trading_session_id,
                        principalSchema: "static_data",
                        principalTable: "trading_sessions",
                        principalColumn: "trading_session_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                schema: "static_data",
                columns: table => new
                {
                    contract_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('static_data.contracts_seq')"),
                    ticker = table.Column<string>(type: "text", nullable: false),
                    template_id = table.Column<int>(type: "integer", nullable: false),
                    first_trading_date = table.Column<LocalDate>(type: "date", nullable: true),
                    expiration_date = table.Column<LocalDate>(type: "date", nullable: true),
                    synthetic_contract_type = table.Column<string>(type: "text", nullable: true),
                    synthetic_requires_bar_recalculation_at_rollover = table.Column<bool>(type: "boolean", nullable: true),
                    external_contract_id = table.Column<string>(type: "text", nullable: true),
                    asset_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    default_datafeed_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contracts", x => x.contract_id);
                    table.ForeignKey(
                        name: "FK_contracts_assets_asset_id",
                        column: x => x.asset_id,
                        principalSchema: "static_data",
                        principalTable: "assets",
                        principalColumn: "asset_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contracts_contract_templates_template_id",
                        column: x => x.template_id,
                        principalSchema: "static_data",
                        principalTable: "contract_templates",
                        principalColumn: "template_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "balance_operations",
                schema: "history",
                columns: table => new
                {
                    balance_operation_id = table.Column<int>(type: "integer", nullable: false),
                    account_service_name = table.Column<string>(type: "text", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    dt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    asset_id = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    fx_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    value_in_account_ccy = table.Column<decimal>(type: "numeric", nullable: false),
                    external_id = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_correction = table.Column<bool>(type: "boolean", nullable: false),
                    affects_pnl = table.Column<bool>(type: "boolean", nullable: false),
                    affects_investment = table.Column<bool>(type: "boolean", nullable: false),
                    affects_balance = table.Column<bool>(type: "boolean", nullable: false),
                    affects_share_count = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_balance_operations", x => new { x.account_service_name, x.balance_operation_id });
                    table.ForeignKey(
                        name: "FK_balance_operations_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_balance_operations_assets_asset_id",
                        column: x => x.asset_id,
                        principalSchema: "static_data",
                        principalTable: "assets",
                        principalColumn: "asset_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "strategies",
                schema: "entities",
                columns: table => new
                {
                    strategy_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('entities.strategies_seq')"),
                    @params = table.Column<string>(name: "params", type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    class_name = table.Column<string>(type: "text", nullable: false),
                    required_bar_storages = table.Column<IReadOnlyDictionary<string, BarStorageConfig>>(type: "jsonb", nullable: false),
                    symbols = table.Column<IReadOnlyDictionary<string, int>>(type: "jsonb", nullable: false),
                    liquidation_parameters = table.Column<LiquidationParameters>(type: "jsonb", nullable: true),
                    use_signal_groups = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    strategies_service = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_strategies", x => x.strategy_id);
                    table.ForeignKey(
                        name: "FK_strategies_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_strategies_ss_instances_strategies_service",
                        column: x => x.strategies_service,
                        principalSchema: "infrastructure",
                        principalTable: "ss_instances",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subaccounts",
                schema: "entities",
                columns: table => new
                {
                    subaccount_history_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('entities.subaccounts_seq')"),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    subaccount_id = table.Column<int>(type: "integer", nullable: false),
                    classifier = table.Column<string>(type: "text", nullable: false),
                    broker_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subaccounts", x => x.subaccount_history_id);
                    table.ForeignKey(
                        name: "FK_subaccounts_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subaccounts_accounts_subaccount_id",
                        column: x => x.subaccount_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subaccounts_brokers_broker_id",
                        column: x => x.broker_id,
                        principalSchema: "static_data",
                        principalTable: "brokers",
                        principalColumn: "broker_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trading_clients",
                schema: "entities",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    execution_service = table.Column<string>(type: "text", nullable: false),
                    external_account_id = table.Column<string>(type: "text", nullable: true),
                    class_name = table.Column<string>(type: "text", nullable: false),
                    @params = table.Column<string>(name: "params", type: "text", nullable: false),
                    secret = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trading_clients", x => x.account_id);
                    table.ForeignKey(
                        name: "FK_trading_clients_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trading_clients_es_instances_execution_service",
                        column: x => x.execution_service,
                        principalSchema: "infrastructure",
                        principalTable: "es_instances",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "binance_usdm_ob_subscriptions",
                schema: "market_data",
                columns: table => new
                {
                    subscription_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    symbol = table.Column<string>(type: "text", nullable: false),
                    frequency = table.Column<int>(type: "integer", nullable: false),
                    levels = table.Column<int>(type: "integer", nullable: false),
                    client_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_binance_usdm_ob_subscriptions", x => x.subscription_id);
                    table.ForeignKey(
                        name: "FK_binance_usdm_ob_subscriptions_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "static_data",
                        principalTable: "contracts",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_binance_usdm_ob_subscriptions_market_data_clients_client_na~",
                        column: x => x.client_name,
                        principalSchema: "infrastructure",
                        principalTable: "market_data_clients",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fx_conversion_contracts",
                schema: "static_data",
                columns: table => new
                {
                    contract_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fx_conversion_contracts", x => x.contract_id);
                    table.ForeignKey(
                        name: "FK_fx_conversion_contracts_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "static_data",
                        principalTable: "contracts",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orders_history",
                schema: "history",
                columns: table => new
                {
                    account_service_name = table.Column<string>(type: "text", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    exec_id = table.Column<long>(type: "bigint", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    broker_account_id = table.Column<int>(type: "integer", nullable: true),
                    transact_time = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    cl_ord_id = table.Column<string>(type: "text", nullable: true),
                    external_id = table.Column<string>(type: "text", nullable: true),
                    execution_request_id = table.Column<long>(type: "bigint", nullable: true),
                    signal_group_id = table.Column<long>(type: "bigint", nullable: true),
                    request_id = table.Column<string>(type: "text", nullable: true),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    strategy_position_id = table.Column<string>(type: "text", nullable: true),
                    position_effect = table.Column<string>(type: "text", nullable: true),
                    ord_type = table.Column<string>(type: "text", nullable: false),
                    side = table.Column<string>(type: "text", nullable: false),
                    order_qty = table.Column<decimal>(type: "numeric", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: true),
                    stop_px = table.Column<decimal>(type: "numeric", nullable: true),
                    ord_status = table.Column<string>(type: "text", nullable: false),
                    exec_type = table.Column<string>(type: "text", nullable: false),
                    exec_type_reason = table.Column<string>(type: "text", nullable: true),
                    cum_qty = table.Column<decimal>(type: "numeric", nullable: false),
                    leaves_qty = table.Column<decimal>(type: "numeric", nullable: false),
                    last_px = table.Column<decimal>(type: "numeric", nullable: true),
                    last_qty = table.Column<decimal>(type: "numeric", nullable: true),
                    calculated_ccy_last_qty = table.Column<decimal>(type: "numeric", nullable: true),
                    time_in_force = table.Column<string>(type: "text", nullable: false),
                    is_suspended = table.Column<bool>(type: "boolean", nullable: false),
                    activation_dt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    expire_dt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    linked_orders = table.Column<IReadOnlyDictionary<string, LinkType>>(type: "jsonb", nullable: false),
                    trading_sessions_ids = table.Column<IReadOnlyCollection<int>>(type: "jsonb", nullable: true),
                    peg_instructions = table.Column<PegInstructions>(type: "jsonb", nullable: true),
                    is_virtual = table.Column<bool>(type: "boolean", nullable: false),
                    is_sltp = table.Column<bool>(type: "boolean", nullable: false),
                    exec_inst = table.Column<IReadOnlyCollection<ExecInst>>(type: "jsonb", nullable: false),
                    reject_reason = table.Column<string>(type: "text", nullable: true),
                    reject_text = table.Column<string>(type: "text", nullable: true),
                    parent_position_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders_history", x => new { x.account_service_name, x.exec_id });
                    table.ForeignKey(
                        name: "FK_orders_history_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_history_accounts_broker_account_id",
                        column: x => x.broker_account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_history_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "static_data",
                        principalTable: "contracts",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "streams",
                schema: "static_data",
                columns: table => new
                {
                    stream_id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('static_data.streams_seq')"),
                    ticker = table.Column<string>(type: "text", nullable: true),
                    datafeed_id = table.Column<int>(type: "integer", nullable: false),
                    contract_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_streams", x => x.stream_id);
                    table.ForeignKey(
                        name: "FK_streams_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "static_data",
                        principalTable: "contracts",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trades",
                schema: "history",
                columns: table => new
                {
                    account_service_name = table.Column<string>(type: "text", nullable: false),
                    trade_id = table.Column<long>(type: "bigint", nullable: false),
                    orig_trade_id = table.Column<long>(type: "bigint", nullable: true),
                    cl_ord_id = table.Column<string>(type: "text", nullable: true),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: true),
                    exec_id = table.Column<long>(type: "bigint", nullable: true),
                    strategy_position_id = table.Column<string>(type: "text", nullable: true),
                    signal_group_id = table.Column<long>(type: "bigint", nullable: true),
                    position_effect = table.Column<string>(type: "text", nullable: true),
                    side = table.Column<string>(type: "text", nullable: false),
                    volume = table.Column<decimal>(type: "numeric", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    commission = table.Column<decimal>(type: "numeric", nullable: false),
                    dt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    execution_request_id = table.Column<long>(type: "bigint", nullable: true),
                    external_trade_id = table.Column<string>(type: "text", nullable: true),
                    commissions = table.Column<IReadOnlyDictionary<int, decimal>>(type: "jsonb", nullable: false),
                    payment_currency_id = table.Column<int>(type: "integer", nullable: false),
                    fx_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    calculated_ccy_last_qty = table.Column<decimal>(type: "numeric", nullable: false),
                    parent_position_id = table.Column<long>(type: "bigint", nullable: true),
                    is_synthetic = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trades", x => new { x.account_service_name, x.trade_id });
                    table.ForeignKey(
                        name: "FK_trades_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trades_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "static_data",
                        principalTable: "contracts",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trades_currencies_payment_currency_id",
                        column: x => x.payment_currency_id,
                        principalSchema: "static_data",
                        principalTable: "currencies",
                        principalColumn: "currency_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trades_orders_history_account_service_name_exec_id",
                        columns: x => new { x.account_service_name, x.exec_id },
                        principalSchema: "history",
                        principalTable: "orders_history",
                        principalColumns: new[] { "account_service_name", "exec_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "binance_usdm_subscriptions",
                schema: "market_data",
                columns: table => new
                {
                    subscription_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stream_id = table.Column<int>(type: "integer", nullable: true),
                    subscription_type = table.Column<string>(type: "text", nullable: false),
                    symbol = table.Column<string>(type: "text", nullable: false),
                    client_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_binance_usdm_subscriptions", x => x.subscription_id);
                    table.ForeignKey(
                        name: "FK_binance_usdm_subscriptions_market_data_clients_client_name",
                        column: x => x.client_name,
                        principalSchema: "infrastructure",
                        principalTable: "market_data_clients",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_binance_usdm_subscriptions_streams_stream_id",
                        column: x => x.stream_id,
                        principalSchema: "static_data",
                        principalTable: "streams",
                        principalColumn: "stream_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "constant_value_streams",
                schema: "static_data",
                columns: table => new
                {
                    stream_id = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_constant_value_streams", x => x.stream_id);
                    table.ForeignKey(
                        name: "FK_constant_value_streams_streams_stream_id",
                        column: x => x.stream_id,
                        principalSchema: "static_data",
                        principalTable: "streams",
                        principalColumn: "stream_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ibkr_subscriptions",
                schema: "market_data",
                columns: table => new
                {
                    subscription_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    con_id = table.Column<int>(type: "integer", nullable: false),
                    ticker = table.Column<string>(type: "text", nullable: false),
                    security_type = table.Column<string>(type: "text", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    exchange = table.Column<string>(type: "text", nullable: false),
                    futures_last_date = table.Column<string>(type: "text", nullable: false),
                    local_symbol = table.Column<string>(type: "text", nullable: false),
                    subscription_type = table.Column<string>(type: "text", nullable: false),
                    use_rth = table.Column<bool>(type: "boolean", nullable: false),
                    stream_id = table.Column<int>(type: "integer", nullable: true),
                    client_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ibkr_subscriptions", x => x.subscription_id);
                    table.ForeignKey(
                        name: "FK_ibkr_subscriptions_market_data_clients_client_name",
                        column: x => x.client_name,
                        principalSchema: "infrastructure",
                        principalTable: "market_data_clients",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_subscriptions_streams_stream_id",
                        column: x => x.stream_id,
                        principalSchema: "static_data",
                        principalTable: "streams",
                        principalColumn: "stream_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "events",
                schema: "events",
                columns: table => new
                {
                    account_service_name = table.Column<string>(type: "text", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    ts = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: true),
                    strategy_id = table.Column<int>(type: "integer", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    balance_operation_id = table.Column<int>(type: "integer", nullable: true),
                    exec_id = table.Column<long>(type: "bigint", nullable: true),
                    trade_id = table.Column<long>(type: "bigint", nullable: true),
                    subaccount_id = table.Column<int>(type: "integer", nullable: true),
                    data = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => new { x.account_service_name, x.event_id });
                    table.ForeignKey(
                        name: "FK_events_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_events_as_instances_account_service_name",
                        column: x => x.account_service_name,
                        principalSchema: "infrastructure",
                        principalTable: "as_instances",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_events_balance_operations_account_service_name_balance_oper~",
                        columns: x => new { x.account_service_name, x.balance_operation_id },
                        principalSchema: "history",
                        principalTable: "balance_operations",
                        principalColumns: new[] { "account_service_name", "balance_operation_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_events_orders_history_account_service_name_exec_id",
                        columns: x => new { x.account_service_name, x.exec_id },
                        principalSchema: "history",
                        principalTable: "orders_history",
                        principalColumns: new[] { "account_service_name", "exec_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_events_strategies_strategy_id",
                        column: x => x.strategy_id,
                        principalSchema: "entities",
                        principalTable: "strategies",
                        principalColumn: "strategy_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_events_subaccounts_subaccount_id",
                        column: x => x.subaccount_id,
                        principalSchema: "entities",
                        principalTable: "subaccounts",
                        principalColumn: "subaccount_history_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_events_trades_account_service_name_trade_id",
                        columns: x => new { x.account_service_name, x.trade_id },
                        principalSchema: "history",
                        principalTable: "trades",
                        principalColumns: new[] { "account_service_name", "trade_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "end_of_day_balances",
                schema: "history",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    currency_id = table.Column<int>(type: "integer", nullable: false),
                    dt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    as_name = table.Column<string>(type: "text", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: true),
                    cash_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    holdings = table.Column<decimal>(type: "numeric", nullable: false),
                    unrealized_pnl = table.Column<decimal>(type: "numeric", nullable: false),
                    futures_vm = table.Column<decimal>(type: "numeric", nullable: false),
                    total_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    total_value = table.Column<decimal>(type: "numeric", nullable: false),
                    fx_rate = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_end_of_day_balances", x => new { x.as_name, x.account_id, x.currency_id, x.dt });
                    table.ForeignKey(
                        name: "FK_end_of_day_balances_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_end_of_day_balances_as_instances_as_name",
                        column: x => x.as_name,
                        principalSchema: "infrastructure",
                        principalTable: "as_instances",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_end_of_day_balances_currencies_currency_id",
                        column: x => x.currency_id,
                        principalSchema: "static_data",
                        principalTable: "currencies",
                        principalColumn: "currency_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_end_of_day_balances_events_as_name_event_id",
                        columns: x => new { x.as_name, x.event_id },
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumns: new[] { "account_service_name", "event_id" },
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "end_of_day_positions",
                schema: "events",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    position_id = table.Column<long>(type: "bigint", nullable: false),
                    dt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    signed_value = table.Column<decimal>(type: "numeric", nullable: false),
                    fx_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    signed_value_in_account_ccy = table.Column<decimal>(type: "numeric", nullable: false),
                    equity_value_in_account_ccy = table.Column<decimal>(type: "numeric", nullable: false),
                    account_service_name = table.Column<string>(type: "text", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_end_of_day_positions", x => new { x.account_id, x.position_id, x.dt });
                    table.ForeignKey(
                        name: "FK_end_of_day_positions_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_end_of_day_positions_events_account_service_name_event_id",
                        columns: x => new { x.account_service_name, x.event_id },
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumns: new[] { "account_service_name", "event_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "external_trades",
                schema: "events",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    external_trade_id = table.Column<string>(type: "text", nullable: false),
                    account_service_name = table.Column<string>(type: "text", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false),
                    external_contract_id = table.Column<string>(type: "text", nullable: false),
                    side = table.Column<string>(type: "text", nullable: false),
                    volume = table.Column<decimal>(type: "numeric", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    commission = table.Column<decimal>(type: "numeric", nullable: false),
                    dt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    external_order_id = table.Column<string>(type: "text", nullable: false),
                    commission_currency = table.Column<string>(type: "text", nullable: true),
                    calculated_ccy_last_qty = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_trades", x => new { x.account_id, x.external_trade_id });
                    table.ForeignKey(
                        name: "FK_external_trades_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_external_trades_events_account_service_name_event_id",
                        columns: x => new { x.account_service_name, x.event_id },
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumns: new[] { "account_service_name", "event_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "positions_history",
                schema: "projections",
                columns: table => new
                {
                    position_history_id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('history.position_history_id_seq')"),
                    account_service_name = table.Column<string>(type: "text", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false),
                    change_type = table.Column<string>(type: "text", nullable: false),
                    open_trade_id = table.Column<long>(type: "bigint", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    strategy_position_id = table.Column<string>(type: "text", nullable: true),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    volume = table.Column<decimal>(type: "numeric", nullable: false),
                    side = table.Column<string>(type: "text", nullable: false),
                    total_open_payments = table.Column<decimal>(type: "numeric", nullable: false),
                    open_dt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    history_open_dt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    total_settl_payments = table.Column<decimal>(type: "numeric", nullable: false),
                    total_settl_payments_in_account_ccy = table.Column<decimal>(type: "numeric", nullable: false),
                    close_trade_id = table.Column<long>(type: "bigint", nullable: true),
                    close_price = table.Column<decimal>(type: "numeric", nullable: true),
                    close_dt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    realized_pnl = table.Column<decimal>(type: "numeric", nullable: false),
                    realized_pnl_in_account_ccy = table.Column<decimal>(type: "numeric", nullable: false),
                    floating_pnl = table.Column<decimal>(type: "numeric", nullable: false),
                    total_floating_pnl = table.Column<decimal>(type: "numeric", nullable: false),
                    commission = table.Column<decimal>(type: "numeric", nullable: false),
                    signal_group_id = table.Column<long>(type: "bigint", nullable: true),
                    is_synthetic = table.Column<bool>(type: "boolean", nullable: false),
                    parent_position_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positions_history", x => x.position_history_id);
                    table.ForeignKey(
                        name: "FK_positions_history_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_positions_history_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "static_data",
                        principalTable: "contracts",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_positions_history_events_account_service_name_event_id",
                        columns: x => new { x.account_service_name, x.event_id },
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumns: new[] { "account_service_name", "event_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_positions_history_trades_account_service_name_close_trade_id",
                        columns: x => new { x.account_service_name, x.close_trade_id },
                        principalSchema: "history",
                        principalTable: "trades",
                        principalColumns: new[] { "account_service_name", "trade_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_positions_history_trades_account_service_name_open_trade_id",
                        columns: x => new { x.account_service_name, x.open_trade_id },
                        principalSchema: "history",
                        principalTable: "trades",
                        principalColumns: new[] { "account_service_name", "trade_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "share_count_updates",
                schema: "events",
                columns: table => new
                {
                    account_service_name = table.Column<string>(type: "text", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    change = table.Column<decimal>(type: "numeric", nullable: false),
                    bo_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_share_count_updates", x => new { x.account_service_name, x.event_id });
                    table.ForeignKey(
                        name: "FK_share_count_updates_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_share_count_updates_as_instances_account_service_name",
                        column: x => x.account_service_name,
                        principalSchema: "infrastructure",
                        principalTable: "as_instances",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_share_count_updates_balance_operations_account_service_name~",
                        columns: x => new { x.account_service_name, x.bo_id },
                        principalSchema: "history",
                        principalTable: "balance_operations",
                        principalColumns: new[] { "account_service_name", "balance_operation_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_share_count_updates_events_account_service_name_event_id",
                        columns: x => new { x.account_service_name, x.event_id },
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumns: new[] { "account_service_name", "event_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "share_price_history",
                schema: "projections",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    dt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    share_count = table.Column<decimal>(type: "numeric", nullable: false),
                    share_price = table.Column<decimal>(type: "numeric", nullable: false),
                    daily_return = table.Column<decimal>(type: "numeric", nullable: false),
                    hwm = table.Column<decimal>(type: "numeric", nullable: false),
                    investment = table.Column<decimal>(type: "numeric", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    account_service_name = table.Column<string>(type: "text", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_share_price_history", x => new { x.account_id, x.dt });
                    table.ForeignKey(
                        name: "FK_share_price_history_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_share_price_history_events_account_service_name_event_id",
                        columns: x => new { x.account_service_name, x.event_id },
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumns: new[] { "account_service_name", "event_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "share_price_updates",
                schema: "events",
                columns: table => new
                {
                    account_service_name = table.Column<string>(type: "text", nullable: false),
                    event_id = table.Column<long>(type: "bigint", nullable: false),
                    account_id = table.Column<int>(type: "integer", nullable: false),
                    equity = table.Column<decimal>(type: "numeric", nullable: false),
                    share_price = table.Column<decimal>(type: "numeric", nullable: false),
                    daily_return = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_share_price_updates", x => new { x.account_service_name, x.event_id });
                    table.ForeignKey(
                        name: "FK_share_price_updates_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "entities",
                        principalTable: "accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_share_price_updates_as_instances_account_service_name",
                        column: x => x.account_service_name,
                        principalSchema: "infrastructure",
                        principalTable: "as_instances",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_share_price_updates_events_account_service_name_event_id",
                        columns: x => new { x.account_service_name, x.event_id },
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumns: new[] { "account_service_name", "event_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "static_data",
                table: "assets",
                columns: new[] { "asset_id", "type", "description", "name" },
                values: new object[] { 840, "Currency", "US Dollar", "USD" });

            migrationBuilder.InsertData(
                schema: "static_data",
                table: "brokers",
                columns: new[] { "broker_id", "broker_type", "name" },
                values: new object[,]
                {
                    { 100, "Ibkr", "Interactive Brokers" },
                    { 101, "BinanceUsdmFutures", "Binance USD-m Futures" }
                });

            migrationBuilder.InsertData(
                schema: "static_data",
                table: "datafeeds",
                columns: new[] { "datafeed_id", "name" },
                values: new object[] { 100, "Default datafeed" });

            migrationBuilder.InsertData(
                schema: "static_data",
                table: "exchanges",
                columns: new[] { "exchange_id", "name", "timezone" },
                values: new object[] { 119, "Binance USD-m Futures", "UTC" });

            migrationBuilder.InsertData(
                schema: "static_data",
                table: "currencies",
                columns: new[] { "currency_id", "decimals" },
                values: new object[] { 840, 2 });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_account_service",
                schema: "entities",
                table: "accounts",
                column: "account_service");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_broker_id",
                schema: "entities",
                table: "accounts",
                column: "broker_id");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_currency_id",
                schema: "entities",
                table: "accounts",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_name",
                schema: "entities",
                table: "accounts",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_as_instances_location_name",
                schema: "infrastructure",
                table: "as_instances",
                column: "location_name");

            migrationBuilder.CreateIndex(
                name: "IX_assets_name",
                schema: "static_data",
                table: "assets",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_balance_operations_account_id_external_id",
                schema: "history",
                table: "balance_operations",
                columns: new[] { "account_id", "external_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", true);

            migrationBuilder.CreateIndex(
                name: "IX_balance_operations_asset_id",
                schema: "history",
                table: "balance_operations",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_binance_usdm_ob_subscriptions_client_name",
                schema: "market_data",
                table: "binance_usdm_ob_subscriptions",
                column: "client_name");

            migrationBuilder.CreateIndex(
                name: "IX_binance_usdm_ob_subscriptions_contract_id",
                schema: "market_data",
                table: "binance_usdm_ob_subscriptions",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_binance_usdm_subscriptions_client_name",
                schema: "market_data",
                table: "binance_usdm_subscriptions",
                column: "client_name");

            migrationBuilder.CreateIndex(
                name: "IX_binance_usdm_subscriptions_stream_id",
                schema: "market_data",
                table: "binance_usdm_subscriptions",
                column: "stream_id");

            migrationBuilder.CreateIndex(
                name: "IX_brokers_name",
                schema: "static_data",
                table: "brokers",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commissions_broker_id",
                schema: "static_data",
                table: "commissions",
                column: "broker_id");

            migrationBuilder.CreateIndex(
                name: "IX_commissions_currency_id",
                schema: "static_data",
                table: "commissions",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_commissions_exchange_id",
                schema: "static_data",
                table: "commissions",
                column: "exchange_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_templates_asset_id",
                schema: "static_data",
                table: "contract_templates",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_templates_base_currency_id",
                schema: "static_data",
                table: "contract_templates",
                column: "base_currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_templates_broker_id",
                schema: "static_data",
                table: "contract_templates",
                column: "broker_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_templates_default_datafeed_id",
                schema: "static_data",
                table: "contract_templates",
                column: "default_datafeed_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_templates_exchange_id",
                schema: "static_data",
                table: "contract_templates",
                column: "exchange_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_templates_name",
                schema: "static_data",
                table: "contract_templates",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contract_templates_quote_currency_id",
                schema: "static_data",
                table: "contract_templates",
                column: "quote_currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_templates_settlement_currency_id",
                schema: "static_data",
                table: "contract_templates",
                column: "settlement_currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_templates_commissions_commission_id",
                schema: "static_data",
                table: "contract_templates_commissions",
                column: "commission_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_templates_trading_sessions_trading_session_id",
                schema: "static_data",
                table: "contract_templates_trading_sessions",
                column: "trading_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_asset_id",
                schema: "static_data",
                table: "contracts",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_template_id",
                schema: "static_data",
                table: "contracts",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_ticker",
                schema: "static_data",
                table: "contracts",
                column: "ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_currency_overrides_broker_id",
                schema: "static_data",
                table: "currency_overrides",
                column: "broker_id");

            migrationBuilder.CreateIndex(
                name: "IX_datafeeds_name",
                schema: "static_data",
                table: "datafeeds",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_end_of_day_balances_account_id",
                schema: "history",
                table: "end_of_day_balances",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_end_of_day_balances_as_name_event_id",
                schema: "history",
                table: "end_of_day_balances",
                columns: new[] { "as_name", "event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_end_of_day_balances_currency_id",
                schema: "history",
                table: "end_of_day_balances",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_end_of_day_positions_account_service_name_event_id",
                schema: "events",
                table: "end_of_day_positions",
                columns: new[] { "account_service_name", "event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_es_instances_location_name",
                schema: "infrastructure",
                table: "es_instances",
                column: "location_name");

            migrationBuilder.CreateIndex(
                name: "IX_events_account_id",
                schema: "events",
                table: "events",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_account_service_name_balance_operation_id",
                schema: "events",
                table: "events",
                columns: new[] { "account_service_name", "balance_operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_events_account_service_name_exec_id",
                schema: "events",
                table: "events",
                columns: new[] { "account_service_name", "exec_id" });

            migrationBuilder.CreateIndex(
                name: "IX_events_account_service_name_trade_id",
                schema: "events",
                table: "events",
                columns: new[] { "account_service_name", "trade_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_events_strategy_id",
                schema: "events",
                table: "events",
                column: "strategy_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_subaccount_id",
                schema: "events",
                table: "events",
                column: "subaccount_id");

            migrationBuilder.CreateIndex(
                name: "IX_external_trades_account_service_name_event_id",
                schema: "events",
                table: "external_trades",
                columns: new[] { "account_service_name", "event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_subscriptions_client_name",
                schema: "market_data",
                table: "ibkr_subscriptions",
                column: "client_name");

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_subscriptions_stream_id",
                schema: "market_data",
                table: "ibkr_subscriptions",
                column: "stream_id");

            migrationBuilder.CreateIndex(
                name: "IX_market_data_clients_location_name",
                schema: "infrastructure",
                table: "market_data_clients",
                column: "location_name");

            migrationBuilder.CreateIndex(
                name: "IX_orders_history_account_id_external_id",
                schema: "history",
                table: "orders_history",
                columns: new[] { "account_id", "external_id" })
                .Annotation("Npgsql:NullsDistinct", true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_history_broker_account_id",
                schema: "history",
                table: "orders_history",
                column: "broker_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_history_contract_id",
                schema: "history",
                table: "orders_history",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_positions_history_account_id",
                schema: "projections",
                table: "positions_history",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_positions_history_account_service_name_close_trade_id",
                schema: "projections",
                table: "positions_history",
                columns: new[] { "account_service_name", "close_trade_id" });

            migrationBuilder.CreateIndex(
                name: "IX_positions_history_account_service_name_event_id",
                schema: "projections",
                table: "positions_history",
                columns: new[] { "account_service_name", "event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_positions_history_account_service_name_open_trade_id",
                schema: "projections",
                table: "positions_history",
                columns: new[] { "account_service_name", "open_trade_id" });

            migrationBuilder.CreateIndex(
                name: "IX_positions_history_contract_id",
                schema: "projections",
                table: "positions_history",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_share_count_updates_account_id",
                schema: "events",
                table: "share_count_updates",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_share_count_updates_account_service_name_bo_id",
                schema: "events",
                table: "share_count_updates",
                columns: new[] { "account_service_name", "bo_id" });

            migrationBuilder.CreateIndex(
                name: "IX_share_price_history_account_service_name_event_id",
                schema: "projections",
                table: "share_price_history",
                columns: new[] { "account_service_name", "event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_share_price_updates_account_id",
                schema: "events",
                table: "share_price_updates",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_ss_instances_location_name",
                schema: "infrastructure",
                table: "ss_instances",
                column: "location_name");

            migrationBuilder.CreateIndex(
                name: "IX_strategies_account_id",
                schema: "entities",
                table: "strategies",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_strategies_name",
                schema: "entities",
                table: "strategies",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_strategies_strategies_service",
                schema: "entities",
                table: "strategies",
                column: "strategies_service");

            migrationBuilder.CreateIndex(
                name: "IX_streams_contract_id",
                schema: "static_data",
                table: "streams",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_subaccounts_account_id",
                schema: "entities",
                table: "subaccounts",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_subaccounts_broker_id",
                schema: "entities",
                table: "subaccounts",
                column: "broker_id");

            migrationBuilder.CreateIndex(
                name: "IX_subaccounts_subaccount_id",
                schema: "entities",
                table: "subaccounts",
                column: "subaccount_id");

            migrationBuilder.CreateIndex(
                name: "IX_trades_account_id",
                schema: "history",
                table: "trades",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_trades_account_service_name_exec_id",
                schema: "history",
                table: "trades",
                columns: new[] { "account_service_name", "exec_id" });

            migrationBuilder.CreateIndex(
                name: "IX_trades_contract_id",
                schema: "history",
                table: "trades",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_trades_payment_currency_id",
                schema: "history",
                table: "trades",
                column: "payment_currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_trading_clients_execution_service",
                schema: "entities",
                table: "trading_clients",
                column: "execution_service");

            migrationBuilder.CreateIndex(
                name: "IX_trading_session_intervals_trading_session_id",
                schema: "static_data",
                table: "trading_session_intervals",
                column: "trading_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_trading_sessions_exchange_id",
                schema: "static_data",
                table: "trading_sessions",
                column: "exchange_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "binance_usdm_ob_subscriptions",
                schema: "market_data");

            migrationBuilder.DropTable(
                name: "binance_usdm_subscriptions",
                schema: "market_data");

            migrationBuilder.DropTable(
                name: "constant_value_streams",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "contract_templates_commissions",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "contract_templates_trading_sessions",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "currency_overrides",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "end_of_day_balances",
                schema: "history");

            migrationBuilder.DropTable(
                name: "end_of_day_positions",
                schema: "events");

            migrationBuilder.DropTable(
                name: "external_trades",
                schema: "events");

            migrationBuilder.DropTable(
                name: "fx_conversion_contracts",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "ibkr_subscriptions",
                schema: "market_data");

            migrationBuilder.DropTable(
                name: "positions_history",
                schema: "projections");

            migrationBuilder.DropTable(
                name: "share_count_updates",
                schema: "events");

            migrationBuilder.DropTable(
                name: "share_price_history",
                schema: "projections");

            migrationBuilder.DropTable(
                name: "share_price_updates",
                schema: "events");

            migrationBuilder.DropTable(
                name: "trading_clients",
                schema: "entities");

            migrationBuilder.DropTable(
                name: "trading_session_intervals",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "commissions",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "market_data_clients",
                schema: "infrastructure");

            migrationBuilder.DropTable(
                name: "streams",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "events",
                schema: "events");

            migrationBuilder.DropTable(
                name: "es_instances",
                schema: "infrastructure");

            migrationBuilder.DropTable(
                name: "trading_sessions",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "balance_operations",
                schema: "history");

            migrationBuilder.DropTable(
                name: "strategies",
                schema: "entities");

            migrationBuilder.DropTable(
                name: "subaccounts",
                schema: "entities");

            migrationBuilder.DropTable(
                name: "trades",
                schema: "history");

            migrationBuilder.DropTable(
                name: "ss_instances",
                schema: "infrastructure");

            migrationBuilder.DropTable(
                name: "orders_history",
                schema: "history");

            migrationBuilder.DropTable(
                name: "accounts",
                schema: "entities");

            migrationBuilder.DropTable(
                name: "contracts",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "as_instances",
                schema: "infrastructure");

            migrationBuilder.DropTable(
                name: "contract_templates",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "locations",
                schema: "infrastructure");

            migrationBuilder.DropTable(
                name: "brokers",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "currencies",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "datafeeds",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "exchanges",
                schema: "static_data");

            migrationBuilder.DropTable(
                name: "assets",
                schema: "static_data");

            migrationBuilder.DropSequence(
                name: "accounts_seq",
                schema: "entities");

            migrationBuilder.DropSequence(
                name: "assets_seq",
                schema: "static_data");

            migrationBuilder.DropSequence(
                name: "brokers_seq",
                schema: "static_data");

            migrationBuilder.DropSequence(
                name: "commissions_seq",
                schema: "static_data");

            migrationBuilder.DropSequence(
                name: "contract_templates_seq",
                schema: "static_data");

            migrationBuilder.DropSequence(
                name: "contracts_seq",
                schema: "static_data");

            migrationBuilder.DropSequence(
                name: "datafeeds_seq",
                schema: "static_data");

            migrationBuilder.DropSequence(
                name: "exchanges_seq",
                schema: "static_data");

            migrationBuilder.DropSequence(
                name: "position_history_id_seq",
                schema: "history");

            migrationBuilder.DropSequence(
                name: "strategies_seq",
                schema: "entities");

            migrationBuilder.DropSequence(
                name: "streams_seq",
                schema: "static_data");

            migrationBuilder.DropSequence(
                name: "subaccounts_seq",
                schema: "entities");

            migrationBuilder.DropSequence(
                name: "trading_sessions_seq",
                schema: "static_data");

            migrationBuilder.DropSequence(
                name: "ts_intervals_seq",
                schema: "static_data");
        }
    }
}

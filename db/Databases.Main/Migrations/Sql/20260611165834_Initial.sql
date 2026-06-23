CREATE TABLE IF NOT EXISTS public.migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK_migrations_history" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'entities') THEN
        CREATE SCHEMA entities;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'infrastructure') THEN
        CREATE SCHEMA infrastructure;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'static_data') THEN
        CREATE SCHEMA static_data;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'history') THEN
        CREATE SCHEMA history;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'market_data') THEN
        CREATE SCHEMA market_data;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'events') THEN
        CREATE SCHEMA events;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'projections') THEN
        CREATE SCHEMA projections;
    END IF;
END $EF$;

CREATE SEQUENCE entities.accounts_seq AS integer START WITH 1000000 INCREMENT BY 1 NO CYCLE;

CREATE SEQUENCE static_data.assets_seq AS integer START WITH 100000 INCREMENT BY 1 NO CYCLE;

CREATE SEQUENCE static_data.brokers_seq AS integer START WITH 100 INCREMENT BY 1 NO CYCLE;

CREATE SEQUENCE static_data.commissions_seq AS integer START WITH 1000 INCREMENT BY 1 NO CYCLE;

CREATE SEQUENCE static_data.contract_templates_seq AS integer START WITH 10000 INCREMENT BY 1 NO CYCLE;

CREATE SEQUENCE static_data.contracts_seq AS integer START WITH 100000 INCREMENT BY 1 NO CYCLE;

CREATE SEQUENCE static_data.datafeeds_seq AS integer START WITH 100 INCREMENT BY 1 NO CYCLE;

CREATE SEQUENCE static_data.exchanges_seq AS integer START WITH 100 INCREMENT BY 1 NO CYCLE;

CREATE SEQUENCE history.position_history_id_seq START WITH 100000000 INCREMENT BY 1 NO CYCLE;

CREATE SEQUENCE entities.strategies_seq AS integer START WITH 50000 INCREMENT BY 1 NO CYCLE;

CREATE SEQUENCE static_data.streams_seq AS integer START WITH 1000000 INCREMENT BY 1 NO CYCLE;

CREATE SEQUENCE entities.subaccounts_seq AS integer START WITH 1000000 INCREMENT BY 1 NO CYCLE;

CREATE SEQUENCE static_data.trading_sessions_seq AS integer START WITH 1000 INCREMENT BY 1 NO CYCLE;

CREATE SEQUENCE static_data.ts_intervals_seq AS integer START WITH 10000 INCREMENT BY 1 NO CYCLE;

CREATE TABLE static_data.assets (
    asset_id integer NOT NULL DEFAULT (nextval('static_data.assets_seq')),
    name text NOT NULL,
    description text,
    type text NOT NULL,
    CONSTRAINT "PK_assets" PRIMARY KEY (asset_id)
);

CREATE TABLE static_data.brokers (
    broker_id integer NOT NULL DEFAULT (nextval('static_data.brokers_seq')),
    name text NOT NULL,
    broker_type text NOT NULL,
    CONSTRAINT "PK_brokers" PRIMARY KEY (broker_id)
);

CREATE TABLE static_data.datafeeds (
    datafeed_id integer NOT NULL DEFAULT (nextval('static_data.datafeeds_seq')),
    name text NOT NULL,
    CONSTRAINT "PK_datafeeds" PRIMARY KEY (datafeed_id)
);

CREATE TABLE static_data.exchanges (
    exchange_id integer NOT NULL DEFAULT (nextval('static_data.exchanges_seq')),
    name text NOT NULL,
    timezone text NOT NULL,
    CONSTRAINT "PK_exchanges" PRIMARY KEY (exchange_id)
);

CREATE TABLE infrastructure.locations (
    name text NOT NULL,
    CONSTRAINT "PK_locations" PRIMARY KEY (name)
);

CREATE TABLE static_data.currencies (
    currency_id integer NOT NULL,
    decimals integer NOT NULL,
    CONSTRAINT "PK_currencies" PRIMARY KEY (currency_id),
    CONSTRAINT "FK_currencies_assets_currency_id" FOREIGN KEY (currency_id) REFERENCES static_data.assets (asset_id) ON DELETE RESTRICT
);

CREATE TABLE static_data.trading_sessions (
    trading_session_id integer NOT NULL DEFAULT (nextval('static_data.trading_sessions_seq')),
    name text NOT NULL,
    exchange_id integer NOT NULL,
    is_24x7 boolean NOT NULL,
    is_rth boolean NOT NULL,
    CONSTRAINT "PK_trading_sessions" PRIMARY KEY (trading_session_id),
    CONSTRAINT "FK_trading_sessions_exchanges_exchange_id" FOREIGN KEY (exchange_id) REFERENCES static_data.exchanges (exchange_id) ON DELETE RESTRICT
);

CREATE TABLE infrastructure.as_instances (
    name text NOT NULL,
    location_name text NOT NULL,
    CONSTRAINT "PK_as_instances" PRIMARY KEY (name),
    CONSTRAINT "FK_as_instances_locations_location_name" FOREIGN KEY (location_name) REFERENCES infrastructure.locations (name) ON DELETE RESTRICT
);

CREATE TABLE infrastructure.es_instances (
    name text NOT NULL,
    location_name text NOT NULL,
    CONSTRAINT "PK_es_instances" PRIMARY KEY (name),
    CONSTRAINT "FK_es_instances_locations_location_name" FOREIGN KEY (location_name) REFERENCES infrastructure.locations (name) ON DELETE RESTRICT
);

CREATE TABLE infrastructure.market_data_clients (
    name text NOT NULL,
    location_name text NOT NULL,
    CONSTRAINT "PK_market_data_clients" PRIMARY KEY (name),
    CONSTRAINT "FK_market_data_clients_locations_location_name" FOREIGN KEY (location_name) REFERENCES infrastructure.locations (name) ON DELETE RESTRICT
);

CREATE TABLE infrastructure.ss_instances (
    name text NOT NULL,
    location_name text NOT NULL,
    CONSTRAINT "PK_ss_instances" PRIMARY KEY (name),
    CONSTRAINT "FK_ss_instances_locations_location_name" FOREIGN KEY (location_name) REFERENCES infrastructure.locations (name) ON DELETE RESTRICT
);

CREATE TABLE static_data.commissions (
    commission_id integer NOT NULL DEFAULT (nextval('static_data.commissions_seq')),
    name text NOT NULL,
    description text,
    fixed_per_share numeric NOT NULL,
    floating numeric NOT NULL,
    currency_id integer NOT NULL,
    commission_structure_type text NOT NULL,
    broker_id integer,
    exchange_id integer,
    CONSTRAINT "PK_commissions" PRIMARY KEY (commission_id),
    CONSTRAINT "FK_commissions_brokers_broker_id" FOREIGN KEY (broker_id) REFERENCES static_data.brokers (broker_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_commissions_currencies_currency_id" FOREIGN KEY (currency_id) REFERENCES static_data.currencies (currency_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_commissions_exchanges_exchange_id" FOREIGN KEY (exchange_id) REFERENCES static_data.exchanges (exchange_id) ON DELETE RESTRICT
);

CREATE TABLE static_data.contract_templates (
    template_id integer NOT NULL DEFAULT (nextval('static_data.contract_templates_seq')),
    name text NOT NULL,
    security_type text NOT NULL,
    pl_calculator_type text NOT NULL,
    asset_id integer,
    min_size numeric NOT NULL,
    min_size_money numeric,
    max_size numeric NOT NULL,
    max_size_money numeric,
    size_increment numeric NOT NULL,
    tick_size numeric NOT NULL,
    tick_value numeric NOT NULL,
    price_quotation numeric NOT NULL,
    settlement_currency_id integer NOT NULL,
    base_currency_id integer,
    quote_currency_id integer,
    default_datafeed_id integer,
    exchange_id integer NOT NULL,
    broker_id integer NOT NULL,
    days_in_year integer NOT NULL,
    description text,
    CONSTRAINT "PK_contract_templates" PRIMARY KEY (template_id),
    CONSTRAINT "FK_contract_templates_assets_asset_id" FOREIGN KEY (asset_id) REFERENCES static_data.assets (asset_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_contract_templates_brokers_broker_id" FOREIGN KEY (broker_id) REFERENCES static_data.brokers (broker_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_contract_templates_currencies_base_currency_id" FOREIGN KEY (base_currency_id) REFERENCES static_data.currencies (currency_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_contract_templates_currencies_quote_currency_id" FOREIGN KEY (quote_currency_id) REFERENCES static_data.currencies (currency_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_contract_templates_currencies_settlement_currency_id" FOREIGN KEY (settlement_currency_id) REFERENCES static_data.currencies (currency_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_contract_templates_datafeeds_default_datafeed_id" FOREIGN KEY (default_datafeed_id) REFERENCES static_data.datafeeds (datafeed_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_contract_templates_exchanges_exchange_id" FOREIGN KEY (exchange_id) REFERENCES static_data.exchanges (exchange_id) ON DELETE RESTRICT
);

CREATE TABLE static_data.currency_overrides (
    currency_id integer NOT NULL,
    broker_id integer NOT NULL,
    decimals integer NOT NULL,
    CONSTRAINT "PK_currency_overrides" PRIMARY KEY (currency_id, broker_id),
    CONSTRAINT "FK_currency_overrides_brokers_broker_id" FOREIGN KEY (broker_id) REFERENCES static_data.brokers (broker_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_currency_overrides_currencies_currency_id" FOREIGN KEY (currency_id) REFERENCES static_data.currencies (currency_id) ON DELETE RESTRICT
);

CREATE TABLE static_data.trading_session_intervals (
    interval_id integer NOT NULL DEFAULT (nextval('static_data.ts_intervals_seq')),
    trading_session_id integer NOT NULL,
    start_day integer NOT NULL,
    start time NOT NULL,
    end_day integer NOT NULL,
    "end" time NOT NULL,
    CONSTRAINT "PK_trading_session_intervals" PRIMARY KEY (interval_id),
    CONSTRAINT "FK_trading_session_intervals_trading_sessions_trading_session_~" FOREIGN KEY (trading_session_id) REFERENCES static_data.trading_sessions (trading_session_id) ON DELETE RESTRICT
);

CREATE TABLE entities.accounts (
    account_id integer NOT NULL DEFAULT (nextval('entities.accounts_seq')),
    account_service text NOT NULL,
    name text NOT NULL,
    currency_id integer NOT NULL,
    account_type text NOT NULL,
    position_accounting text NOT NULL,
    broker_id integer,
    enable_share_price_tracking boolean NOT NULL,
    include_unrealized_pnl_to_mtm boolean NOT NULL,
    CONSTRAINT "PK_accounts" PRIMARY KEY (account_id),
    CONSTRAINT "FK_accounts_as_instances_account_service" FOREIGN KEY (account_service) REFERENCES infrastructure.as_instances (name) ON DELETE RESTRICT,
    CONSTRAINT "FK_accounts_brokers_broker_id" FOREIGN KEY (broker_id) REFERENCES static_data.brokers (broker_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_accounts_currencies_currency_id" FOREIGN KEY (currency_id) REFERENCES static_data.currencies (currency_id) ON DELETE RESTRICT
);

CREATE TABLE static_data.contract_templates_commissions (
    contract_template_id integer NOT NULL,
    commission_id integer NOT NULL,
    CONSTRAINT "PK_contract_templates_commissions" PRIMARY KEY (contract_template_id, commission_id),
    CONSTRAINT "FK_contract_templates_commissions_commissions_commission_id" FOREIGN KEY (commission_id) REFERENCES static_data.commissions (commission_id) ON DELETE CASCADE,
    CONSTRAINT "FK_contract_templates_commissions_contract_templates_contract_~" FOREIGN KEY (contract_template_id) REFERENCES static_data.contract_templates (template_id) ON DELETE CASCADE
);

CREATE TABLE static_data.contract_templates_trading_sessions (
    contract_template_id integer NOT NULL,
    trading_session_id integer NOT NULL,
    CONSTRAINT "PK_contract_templates_trading_sessions" PRIMARY KEY (contract_template_id, trading_session_id),
    CONSTRAINT "FK_contract_templates_trading_sessions_contract_templates_cont~" FOREIGN KEY (contract_template_id) REFERENCES static_data.contract_templates (template_id) ON DELETE CASCADE,
    CONSTRAINT "FK_contract_templates_trading_sessions_trading_sessions_tradin~" FOREIGN KEY (trading_session_id) REFERENCES static_data.trading_sessions (trading_session_id) ON DELETE CASCADE
);

CREATE TABLE static_data.contracts (
    contract_id integer NOT NULL DEFAULT (nextval('static_data.contracts_seq')),
    ticker text NOT NULL,
    template_id integer NOT NULL,
    first_trading_date date,
    expiration_date date,
    synthetic_contract_type text,
    synthetic_requires_bar_recalculation_at_rollover boolean,
    external_contract_id text,
    asset_id integer,
    description text,
    default_datafeed_id integer NOT NULL,
    CONSTRAINT "PK_contracts" PRIMARY KEY (contract_id),
    CONSTRAINT "FK_contracts_assets_asset_id" FOREIGN KEY (asset_id) REFERENCES static_data.assets (asset_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_contracts_contract_templates_template_id" FOREIGN KEY (template_id) REFERENCES static_data.contract_templates (template_id) ON DELETE RESTRICT
);

CREATE TABLE history.balance_operations (
    balance_operation_id integer NOT NULL,
    account_service_name text NOT NULL,
    account_id integer NOT NULL,
    dt timestamp with time zone NOT NULL,
    amount numeric NOT NULL,
    asset_id integer NOT NULL,
    price numeric NOT NULL,
    fx_rate numeric NOT NULL,
    value_in_account_ccy numeric NOT NULL,
    external_id text,
    description text,
    is_correction boolean NOT NULL,
    affects_pnl boolean NOT NULL,
    affects_investment boolean NOT NULL,
    affects_balance boolean NOT NULL,
    affects_share_count boolean NOT NULL,
    CONSTRAINT "PK_balance_operations" PRIMARY KEY (account_service_name, balance_operation_id),
    CONSTRAINT "FK_balance_operations_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_balance_operations_assets_asset_id" FOREIGN KEY (asset_id) REFERENCES static_data.assets (asset_id) ON DELETE RESTRICT
);

CREATE TABLE entities.strategies (
    strategy_id integer NOT NULL DEFAULT (nextval('entities.strategies_seq')),
    params text NOT NULL,
    name text NOT NULL,
    class_name text NOT NULL,
    required_bar_storages jsonb NOT NULL,
    symbols jsonb NOT NULL,
    liquidation_parameters jsonb,
    use_signal_groups boolean NOT NULL,
    status text NOT NULL,
    account_id integer NOT NULL,
    strategies_service text,
    CONSTRAINT "PK_strategies" PRIMARY KEY (strategy_id),
    CONSTRAINT "FK_strategies_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_strategies_ss_instances_strategies_service" FOREIGN KEY (strategies_service) REFERENCES infrastructure.ss_instances (name) ON DELETE RESTRICT
);

CREATE TABLE entities.subaccounts (
    subaccount_history_id integer NOT NULL DEFAULT (nextval('entities.subaccounts_seq')),
    is_active boolean NOT NULL,
    account_id integer NOT NULL,
    subaccount_id integer NOT NULL,
    classifier text NOT NULL,
    broker_id integer,
    CONSTRAINT "PK_subaccounts" PRIMARY KEY (subaccount_history_id),
    CONSTRAINT "FK_subaccounts_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_subaccounts_accounts_subaccount_id" FOREIGN KEY (subaccount_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_subaccounts_brokers_broker_id" FOREIGN KEY (broker_id) REFERENCES static_data.brokers (broker_id) ON DELETE RESTRICT
);

CREATE TABLE entities.trading_clients (
    account_id integer NOT NULL,
    execution_service text NOT NULL,
    external_account_id text,
    class_name text NOT NULL,
    params text NOT NULL,
    secret text NOT NULL,
    CONSTRAINT "PK_trading_clients" PRIMARY KEY (account_id),
    CONSTRAINT "FK_trading_clients_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_trading_clients_es_instances_execution_service" FOREIGN KEY (execution_service) REFERENCES infrastructure.es_instances (name) ON DELETE RESTRICT
);

CREATE TABLE market_data.binance_usdm_ob_subscriptions (
    subscription_id integer GENERATED BY DEFAULT AS IDENTITY,
    contract_id integer NOT NULL,
    symbol text NOT NULL,
    frequency integer NOT NULL,
    levels integer NOT NULL,
    client_name text NOT NULL,
    CONSTRAINT "PK_binance_usdm_ob_subscriptions" PRIMARY KEY (subscription_id),
    CONSTRAINT "FK_binance_usdm_ob_subscriptions_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES static_data.contracts (contract_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_binance_usdm_ob_subscriptions_market_data_clients_client_na~" FOREIGN KEY (client_name) REFERENCES infrastructure.market_data_clients (name) ON DELETE RESTRICT
);

CREATE TABLE static_data.fx_conversion_contracts (
    contract_id integer NOT NULL,
    CONSTRAINT "PK_fx_conversion_contracts" PRIMARY KEY (contract_id),
    CONSTRAINT "FK_fx_conversion_contracts_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES static_data.contracts (contract_id) ON DELETE RESTRICT
);

CREATE TABLE history.orders_history (
    account_service_name text NOT NULL,
    order_id bigint NOT NULL,
    created_at timestamp with time zone NOT NULL,
    exec_id bigint NOT NULL,
    account_id integer NOT NULL,
    broker_account_id integer,
    transact_time timestamp with time zone NOT NULL,
    cl_ord_id text,
    external_id text,
    execution_request_id bigint,
    signal_group_id bigint,
    request_id text,
    contract_id integer NOT NULL,
    strategy_position_id text,
    position_effect text,
    ord_type text NOT NULL,
    side text NOT NULL,
    order_qty numeric NOT NULL,
    price numeric,
    stop_px numeric,
    ord_status text NOT NULL,
    exec_type text NOT NULL,
    exec_type_reason text,
    cum_qty numeric NOT NULL,
    leaves_qty numeric NOT NULL,
    last_px numeric,
    last_qty numeric,
    calculated_ccy_last_qty numeric,
    time_in_force text NOT NULL,
    is_suspended boolean NOT NULL,
    activation_dt timestamp with time zone,
    expire_dt timestamp with time zone,
    linked_orders jsonb NOT NULL,
    trading_sessions_ids jsonb,
    peg_instructions jsonb,
    is_virtual boolean NOT NULL,
    is_sltp boolean NOT NULL,
    exec_inst jsonb NOT NULL,
    reject_reason text,
    reject_text text,
    parent_position_id bigint,
    CONSTRAINT "PK_orders_history" PRIMARY KEY (account_service_name, exec_id),
    CONSTRAINT "FK_orders_history_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_orders_history_accounts_broker_account_id" FOREIGN KEY (broker_account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_orders_history_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES static_data.contracts (contract_id) ON DELETE RESTRICT
);

CREATE TABLE static_data.streams (
    stream_id integer NOT NULL DEFAULT (nextval('static_data.streams_seq')),
    ticker text,
    datafeed_id integer NOT NULL,
    contract_id integer,
    CONSTRAINT "PK_streams" PRIMARY KEY (stream_id),
    CONSTRAINT "FK_streams_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES static_data.contracts (contract_id) ON DELETE RESTRICT
);

CREATE TABLE history.trades (
    account_service_name text NOT NULL,
    trade_id bigint NOT NULL,
    orig_trade_id bigint,
    cl_ord_id text,
    account_id integer NOT NULL,
    contract_id integer NOT NULL,
    order_id bigint,
    exec_id bigint,
    strategy_position_id text,
    signal_group_id bigint,
    position_effect text,
    side text NOT NULL,
    volume numeric NOT NULL,
    price numeric NOT NULL,
    commission numeric NOT NULL,
    dt timestamp with time zone NOT NULL,
    execution_request_id bigint,
    external_trade_id text,
    commissions jsonb NOT NULL,
    payment_currency_id integer NOT NULL,
    fx_rate numeric NOT NULL,
    calculated_ccy_last_qty numeric NOT NULL,
    parent_position_id bigint,
    is_synthetic boolean NOT NULL,
    CONSTRAINT "PK_trades" PRIMARY KEY (account_service_name, trade_id),
    CONSTRAINT "FK_trades_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_trades_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES static_data.contracts (contract_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_trades_currencies_payment_currency_id" FOREIGN KEY (payment_currency_id) REFERENCES static_data.currencies (currency_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_trades_orders_history_account_service_name_exec_id" FOREIGN KEY (account_service_name, exec_id) REFERENCES history.orders_history (account_service_name, exec_id) ON DELETE RESTRICT
);

CREATE TABLE market_data.binance_usdm_subscriptions (
    subscription_id integer GENERATED BY DEFAULT AS IDENTITY,
    stream_id integer,
    subscription_type text NOT NULL,
    symbol text NOT NULL,
    client_name text NOT NULL,
    CONSTRAINT "PK_binance_usdm_subscriptions" PRIMARY KEY (subscription_id),
    CONSTRAINT "FK_binance_usdm_subscriptions_market_data_clients_client_name" FOREIGN KEY (client_name) REFERENCES infrastructure.market_data_clients (name) ON DELETE RESTRICT,
    CONSTRAINT "FK_binance_usdm_subscriptions_streams_stream_id" FOREIGN KEY (stream_id) REFERENCES static_data.streams (stream_id) ON DELETE RESTRICT
);

CREATE TABLE static_data.constant_value_streams (
    stream_id integer NOT NULL,
    value numeric NOT NULL,
    CONSTRAINT "PK_constant_value_streams" PRIMARY KEY (stream_id),
    CONSTRAINT "FK_constant_value_streams_streams_stream_id" FOREIGN KEY (stream_id) REFERENCES static_data.streams (stream_id) ON DELETE RESTRICT
);

CREATE TABLE market_data.ibkr_subscriptions (
    subscription_id integer GENERATED BY DEFAULT AS IDENTITY,
    con_id integer NOT NULL,
    ticker text NOT NULL,
    security_type text NOT NULL,
    currency text NOT NULL,
    exchange text NOT NULL,
    futures_last_date text NOT NULL,
    local_symbol text NOT NULL,
    subscription_type text NOT NULL,
    use_rth boolean NOT NULL,
    stream_id integer,
    client_name text NOT NULL,
    CONSTRAINT "PK_ibkr_subscriptions" PRIMARY KEY (subscription_id),
    CONSTRAINT "FK_ibkr_subscriptions_market_data_clients_client_name" FOREIGN KEY (client_name) REFERENCES infrastructure.market_data_clients (name) ON DELETE RESTRICT,
    CONSTRAINT "FK_ibkr_subscriptions_streams_stream_id" FOREIGN KEY (stream_id) REFERENCES static_data.streams (stream_id) ON DELETE RESTRICT
);

CREATE TABLE events.events (
    account_service_name text NOT NULL,
    event_id bigint NOT NULL,
    event_type text NOT NULL,
    ts timestamp with time zone NOT NULL,
    account_id integer,
    strategy_id integer,
    version bigint NOT NULL,
    balance_operation_id integer,
    exec_id bigint,
    trade_id bigint,
    subaccount_id integer,
    data jsonb,
    CONSTRAINT "PK_events" PRIMARY KEY (account_service_name, event_id),
    CONSTRAINT "FK_events_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_events_as_instances_account_service_name" FOREIGN KEY (account_service_name) REFERENCES infrastructure.as_instances (name) ON DELETE RESTRICT,
    CONSTRAINT "FK_events_balance_operations_account_service_name_balance_oper~" FOREIGN KEY (account_service_name, balance_operation_id) REFERENCES history.balance_operations (account_service_name, balance_operation_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_events_orders_history_account_service_name_exec_id" FOREIGN KEY (account_service_name, exec_id) REFERENCES history.orders_history (account_service_name, exec_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_events_strategies_strategy_id" FOREIGN KEY (strategy_id) REFERENCES entities.strategies (strategy_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_events_subaccounts_subaccount_id" FOREIGN KEY (subaccount_id) REFERENCES entities.subaccounts (subaccount_history_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_events_trades_account_service_name_trade_id" FOREIGN KEY (account_service_name, trade_id) REFERENCES history.trades (account_service_name, trade_id) ON DELETE RESTRICT
);

CREATE TABLE history.end_of_day_balances (
    account_id integer NOT NULL,
    currency_id integer NOT NULL,
    dt timestamp with time zone NOT NULL,
    as_name text NOT NULL,
    event_id bigint,
    cash_balance numeric NOT NULL,
    holdings numeric NOT NULL,
    unrealized_pnl numeric NOT NULL,
    futures_vm numeric NOT NULL,
    total_balance numeric NOT NULL,
    total_value numeric NOT NULL,
    fx_rate numeric NOT NULL,
    CONSTRAINT "PK_end_of_day_balances" PRIMARY KEY (as_name, account_id, currency_id, dt),
    CONSTRAINT "FK_end_of_day_balances_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_end_of_day_balances_as_instances_as_name" FOREIGN KEY (as_name) REFERENCES infrastructure.as_instances (name) ON DELETE RESTRICT,
    CONSTRAINT "FK_end_of_day_balances_currencies_currency_id" FOREIGN KEY (currency_id) REFERENCES static_data.currencies (currency_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_end_of_day_balances_events_as_name_event_id" FOREIGN KEY (as_name, event_id) REFERENCES events.events (account_service_name, event_id) ON DELETE SET NULL
);

CREATE TABLE events.end_of_day_positions (
    account_id integer NOT NULL,
    position_id bigint NOT NULL,
    dt timestamp with time zone NOT NULL,
    price numeric NOT NULL,
    signed_value numeric NOT NULL,
    fx_rate numeric NOT NULL,
    signed_value_in_account_ccy numeric NOT NULL,
    equity_value_in_account_ccy numeric NOT NULL,
    account_service_name text NOT NULL,
    event_id bigint NOT NULL,
    CONSTRAINT "PK_end_of_day_positions" PRIMARY KEY (account_id, position_id, dt),
    CONSTRAINT "FK_end_of_day_positions_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_end_of_day_positions_events_account_service_name_event_id" FOREIGN KEY (account_service_name, event_id) REFERENCES events.events (account_service_name, event_id) ON DELETE RESTRICT
);

CREATE TABLE events.external_trades (
    account_id integer NOT NULL,
    external_trade_id text NOT NULL,
    account_service_name text NOT NULL,
    event_id bigint NOT NULL,
    external_contract_id text NOT NULL,
    side text NOT NULL,
    volume numeric NOT NULL,
    price numeric NOT NULL,
    commission numeric NOT NULL,
    dt timestamp with time zone NOT NULL,
    external_order_id text NOT NULL,
    commission_currency text,
    calculated_ccy_last_qty numeric NOT NULL,
    CONSTRAINT "PK_external_trades" PRIMARY KEY (account_id, external_trade_id),
    CONSTRAINT "FK_external_trades_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_external_trades_events_account_service_name_event_id" FOREIGN KEY (account_service_name, event_id) REFERENCES events.events (account_service_name, event_id) ON DELETE RESTRICT
);

CREATE TABLE projections.positions_history (
    position_history_id bigint NOT NULL DEFAULT (nextval('history.position_history_id_seq')),
    account_service_name text NOT NULL,
    event_id bigint NOT NULL,
    change_type text NOT NULL,
    open_trade_id bigint NOT NULL,
    account_id integer NOT NULL,
    strategy_position_id text,
    contract_id integer NOT NULL,
    volume numeric NOT NULL,
    side text NOT NULL,
    total_open_payments numeric NOT NULL,
    open_dt timestamp with time zone NOT NULL,
    history_open_dt timestamp with time zone NOT NULL,
    total_settl_payments numeric NOT NULL,
    total_settl_payments_in_account_ccy numeric NOT NULL,
    close_trade_id bigint,
    close_price numeric,
    close_dt timestamp with time zone,
    realized_pnl numeric NOT NULL,
    realized_pnl_in_account_ccy numeric NOT NULL,
    floating_pnl numeric NOT NULL,
    total_floating_pnl numeric NOT NULL,
    commission numeric NOT NULL,
    signal_group_id bigint,
    is_synthetic boolean NOT NULL,
    parent_position_id bigint NOT NULL,
    CONSTRAINT "PK_positions_history" PRIMARY KEY (position_history_id),
    CONSTRAINT "FK_positions_history_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_positions_history_contracts_contract_id" FOREIGN KEY (contract_id) REFERENCES static_data.contracts (contract_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_positions_history_events_account_service_name_event_id" FOREIGN KEY (account_service_name, event_id) REFERENCES events.events (account_service_name, event_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_positions_history_trades_account_service_name_close_trade_id" FOREIGN KEY (account_service_name, close_trade_id) REFERENCES history.trades (account_service_name, trade_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_positions_history_trades_account_service_name_open_trade_id" FOREIGN KEY (account_service_name, open_trade_id) REFERENCES history.trades (account_service_name, trade_id) ON DELETE RESTRICT
);

CREATE TABLE events.share_count_updates (
    account_service_name text NOT NULL,
    event_id bigint NOT NULL,
    account_id integer NOT NULL,
    change numeric NOT NULL,
    bo_id integer NOT NULL,
    CONSTRAINT "PK_share_count_updates" PRIMARY KEY (account_service_name, event_id),
    CONSTRAINT "FK_share_count_updates_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_share_count_updates_as_instances_account_service_name" FOREIGN KEY (account_service_name) REFERENCES infrastructure.as_instances (name) ON DELETE RESTRICT,
    CONSTRAINT "FK_share_count_updates_balance_operations_account_service_name~" FOREIGN KEY (account_service_name, bo_id) REFERENCES history.balance_operations (account_service_name, balance_operation_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_share_count_updates_events_account_service_name_event_id" FOREIGN KEY (account_service_name, event_id) REFERENCES events.events (account_service_name, event_id) ON DELETE RESTRICT
);

CREATE TABLE projections.share_price_history (
    account_id integer NOT NULL,
    dt timestamp with time zone NOT NULL,
    share_count numeric NOT NULL,
    share_price numeric NOT NULL,
    daily_return numeric NOT NULL,
    hwm numeric NOT NULL,
    investment numeric NOT NULL,
    type text NOT NULL,
    account_service_name text NOT NULL,
    event_id bigint NOT NULL,
    CONSTRAINT "PK_share_price_history" PRIMARY KEY (account_id, dt),
    CONSTRAINT "FK_share_price_history_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_share_price_history_events_account_service_name_event_id" FOREIGN KEY (account_service_name, event_id) REFERENCES events.events (account_service_name, event_id) ON DELETE RESTRICT
);

CREATE TABLE events.share_price_updates (
    account_service_name text NOT NULL,
    event_id bigint NOT NULL,
    account_id integer NOT NULL,
    equity numeric NOT NULL,
    share_price numeric NOT NULL,
    daily_return numeric NOT NULL,
    CONSTRAINT "PK_share_price_updates" PRIMARY KEY (account_service_name, event_id),
    CONSTRAINT "FK_share_price_updates_accounts_account_id" FOREIGN KEY (account_id) REFERENCES entities.accounts (account_id) ON DELETE RESTRICT,
    CONSTRAINT "FK_share_price_updates_as_instances_account_service_name" FOREIGN KEY (account_service_name) REFERENCES infrastructure.as_instances (name) ON DELETE RESTRICT,
    CONSTRAINT "FK_share_price_updates_events_account_service_name_event_id" FOREIGN KEY (account_service_name, event_id) REFERENCES events.events (account_service_name, event_id) ON DELETE RESTRICT
);

INSERT INTO static_data.assets (asset_id, type, description, name)
VALUES (840, 'Currency', 'US Dollar', 'USD');

INSERT INTO static_data.currencies (currency_id, decimals)
VALUES (840, 2);

CREATE INDEX "IX_accounts_account_service" ON entities.accounts (account_service);

CREATE INDEX "IX_accounts_broker_id" ON entities.accounts (broker_id);

CREATE INDEX "IX_accounts_currency_id" ON entities.accounts (currency_id);

CREATE UNIQUE INDEX "IX_accounts_name" ON entities.accounts (name);

CREATE INDEX "IX_as_instances_location_name" ON infrastructure.as_instances (location_name);

CREATE UNIQUE INDEX "IX_assets_name" ON static_data.assets (name);

CREATE UNIQUE INDEX "IX_balance_operations_account_id_external_id" ON history.balance_operations (account_id, external_id);

CREATE INDEX "IX_balance_operations_asset_id" ON history.balance_operations (asset_id);

CREATE INDEX "IX_binance_usdm_ob_subscriptions_client_name" ON market_data.binance_usdm_ob_subscriptions (client_name);

CREATE INDEX "IX_binance_usdm_ob_subscriptions_contract_id" ON market_data.binance_usdm_ob_subscriptions (contract_id);

CREATE INDEX "IX_binance_usdm_subscriptions_client_name" ON market_data.binance_usdm_subscriptions (client_name);

CREATE INDEX "IX_binance_usdm_subscriptions_stream_id" ON market_data.binance_usdm_subscriptions (stream_id);

CREATE UNIQUE INDEX "IX_brokers_name" ON static_data.brokers (name);

CREATE INDEX "IX_commissions_broker_id" ON static_data.commissions (broker_id);

CREATE INDEX "IX_commissions_currency_id" ON static_data.commissions (currency_id);

CREATE INDEX "IX_commissions_exchange_id" ON static_data.commissions (exchange_id);

CREATE INDEX "IX_contract_templates_asset_id" ON static_data.contract_templates (asset_id);

CREATE INDEX "IX_contract_templates_base_currency_id" ON static_data.contract_templates (base_currency_id);

CREATE INDEX "IX_contract_templates_broker_id" ON static_data.contract_templates (broker_id);

CREATE INDEX "IX_contract_templates_default_datafeed_id" ON static_data.contract_templates (default_datafeed_id);

CREATE INDEX "IX_contract_templates_exchange_id" ON static_data.contract_templates (exchange_id);

CREATE UNIQUE INDEX "IX_contract_templates_name" ON static_data.contract_templates (name);

CREATE INDEX "IX_contract_templates_quote_currency_id" ON static_data.contract_templates (quote_currency_id);

CREATE INDEX "IX_contract_templates_settlement_currency_id" ON static_data.contract_templates (settlement_currency_id);

CREATE INDEX "IX_contract_templates_commissions_commission_id" ON static_data.contract_templates_commissions (commission_id);

CREATE INDEX "IX_contract_templates_trading_sessions_trading_session_id" ON static_data.contract_templates_trading_sessions (trading_session_id);

CREATE INDEX "IX_contracts_asset_id" ON static_data.contracts (asset_id);

CREATE INDEX "IX_contracts_template_id" ON static_data.contracts (template_id);

CREATE UNIQUE INDEX "IX_contracts_ticker" ON static_data.contracts (ticker);

CREATE INDEX "IX_currency_overrides_broker_id" ON static_data.currency_overrides (broker_id);

CREATE UNIQUE INDEX "IX_datafeeds_name" ON static_data.datafeeds (name);

CREATE INDEX "IX_end_of_day_balances_account_id" ON history.end_of_day_balances (account_id);

CREATE INDEX "IX_end_of_day_balances_as_name_event_id" ON history.end_of_day_balances (as_name, event_id);

CREATE INDEX "IX_end_of_day_balances_currency_id" ON history.end_of_day_balances (currency_id);

CREATE INDEX "IX_end_of_day_positions_account_service_name_event_id" ON events.end_of_day_positions (account_service_name, event_id);

CREATE INDEX "IX_es_instances_location_name" ON infrastructure.es_instances (location_name);

CREATE INDEX "IX_events_account_id" ON events.events (account_id);

CREATE UNIQUE INDEX "IX_events_account_service_name_balance_operation_id" ON events.events (account_service_name, balance_operation_id);

CREATE INDEX "IX_events_account_service_name_exec_id" ON events.events (account_service_name, exec_id);

CREATE UNIQUE INDEX "IX_events_account_service_name_trade_id" ON events.events (account_service_name, trade_id);

CREATE INDEX "IX_events_strategy_id" ON events.events (strategy_id);

CREATE INDEX "IX_events_subaccount_id" ON events.events (subaccount_id);

CREATE UNIQUE INDEX "IX_external_trades_account_service_name_event_id" ON events.external_trades (account_service_name, event_id);

CREATE INDEX "IX_ibkr_subscriptions_client_name" ON market_data.ibkr_subscriptions (client_name);

CREATE INDEX "IX_ibkr_subscriptions_stream_id" ON market_data.ibkr_subscriptions (stream_id);

CREATE INDEX "IX_market_data_clients_location_name" ON infrastructure.market_data_clients (location_name);

CREATE INDEX "IX_orders_history_account_id_external_id" ON history.orders_history (account_id, external_id);

CREATE INDEX "IX_orders_history_broker_account_id" ON history.orders_history (broker_account_id);

CREATE INDEX "IX_orders_history_contract_id" ON history.orders_history (contract_id);

CREATE INDEX "IX_positions_history_account_id" ON projections.positions_history (account_id);

CREATE INDEX "IX_positions_history_account_service_name_close_trade_id" ON projections.positions_history (account_service_name, close_trade_id);

CREATE INDEX "IX_positions_history_account_service_name_event_id" ON projections.positions_history (account_service_name, event_id);

CREATE INDEX "IX_positions_history_account_service_name_open_trade_id" ON projections.positions_history (account_service_name, open_trade_id);

CREATE INDEX "IX_positions_history_contract_id" ON projections.positions_history (contract_id);

CREATE INDEX "IX_share_count_updates_account_id" ON events.share_count_updates (account_id);

CREATE INDEX "IX_share_count_updates_account_service_name_bo_id" ON events.share_count_updates (account_service_name, bo_id);

CREATE INDEX "IX_share_price_history_account_service_name_event_id" ON projections.share_price_history (account_service_name, event_id);

CREATE INDEX "IX_share_price_updates_account_id" ON events.share_price_updates (account_id);

CREATE INDEX "IX_ss_instances_location_name" ON infrastructure.ss_instances (location_name);

CREATE UNIQUE INDEX "IX_strategies_account_id" ON entities.strategies (account_id);

CREATE UNIQUE INDEX "IX_strategies_name" ON entities.strategies (name);

CREATE INDEX "IX_strategies_strategies_service" ON entities.strategies (strategies_service);

CREATE INDEX "IX_streams_contract_id" ON static_data.streams (contract_id);

CREATE INDEX "IX_subaccounts_account_id" ON entities.subaccounts (account_id);

CREATE INDEX "IX_subaccounts_broker_id" ON entities.subaccounts (broker_id);

CREATE INDEX "IX_subaccounts_subaccount_id" ON entities.subaccounts (subaccount_id);

CREATE INDEX "IX_trades_account_id" ON history.trades (account_id);

CREATE INDEX "IX_trades_account_service_name_exec_id" ON history.trades (account_service_name, exec_id);

CREATE INDEX "IX_trades_contract_id" ON history.trades (contract_id);

CREATE INDEX "IX_trades_payment_currency_id" ON history.trades (payment_currency_id);

CREATE INDEX "IX_trading_clients_execution_service" ON entities.trading_clients (execution_service);

CREATE INDEX "IX_trading_session_intervals_trading_session_id" ON static_data.trading_session_intervals (trading_session_id);

CREATE INDEX "IX_trading_sessions_exchange_id" ON static_data.trading_sessions (exchange_id);

-- INSERT INTO public.migrations_history ("MigrationId", "ProductVersion")
-- VALUES ('20260611165834_Initial', '9.0.8');
-- 
-- COMMIT;



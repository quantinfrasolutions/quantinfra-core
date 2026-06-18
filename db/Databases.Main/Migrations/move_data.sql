BEGIN TRANSACTION;
CREATE EXTENSION IF NOT EXISTS postgres_fdw;

CREATE SERVER IF NOT EXISTS src
    FOREIGN DATA WRAPPER postgres_fdw
    OPTIONS (host 'localhost', port '5432', dbname 'main_v6');

CREATE USER MAPPING IF NOT EXISTS FOR CURRENT_USER
    SERVER src
    OPTIONS (user 'postgres', password '');


CREATE SCHEMA bkp;

IMPORT FOREIGN SCHEMA public
    FROM SERVER src INTO bkp;


INSERT INTO static_data.assets(asset_id, name, description, type)
SELECT asset_id, name, description, type FROM bkp.assets
WHERE asset_id <> 840;

INSERT INTO static_data.currencies(currency_id, decimals)
SELECT currency_id, decimals FROM bkp.currencies
WHERE currency_id <> 840;

INSERT INTO static_data.brokers(broker_id, name, broker_type)
SELECT broker_id, name, type
FROM bkp.brokers;

INSERT INTO static_data.datafeeds(datafeed_id, name)
SELECT datafeed_id, name FROM bkp.datafeeds;

INSERT INTO static_data.exchanges(exchange_id, name, timezone)
SELECT exchange_id, name, timezone FROM bkp.exchanges;

INSERT INTO static_data.commissions(commission_id, name, description, fixed_per_share, floating, currency_id, commission_structure_type, broker_id, exchange_id)
SELECT id, name, description, fixed_usd_per_share, floating, 840,
       CASE
           WHEN type = 0 THEN 'Broker'
           WHEN type = 1 THEN 'Exchange'
           WHEN type = 2 THEN 'Other'
           WHEN type = 3 THEN 'Slippage'
           END,
       broker_id, exchange_id
FROM bkp.commission_structures;

INSERT INTO static_data.trading_sessions(trading_session_id, name, exchange_id, is_24x7, is_rth)
SELECT trading_session_id, name, exchange_id, is_24x7, is_rth
FROM bkp.trading_sessions;

-- TODO: trading_sessions_days

INSERT INTO static_data.contract_templates(template_id, name, security_type, pl_calculator_type, asset_id, min_size, min_size_money, max_size, max_size_money, size_increment, tick_size, tick_value, price_quotation, settlement_currency_id, base_currency_id, quote_currency_id, default_datafeed_id, exchange_id, broker_id, days_in_year, description)
SELECT template_id, name, type, pl_calculator_type, asset_id, min_size, NULL, max_size, NULL, size_inc, tick_size, tick_value, quotation, settlement_ccy_id, base_ccy_id, quote_ccy_id, default_datafeed_id, exchange_id, broker_id, days_in_year, description
FROM bkp.contract_templates;

INSERT INTO static_data.contract_templates_trading_sessions(contract_template_id, trading_session_id)
SELECT template_id, trading_session_id
FROM bkp.trading_sessions_contracts;

INSERT INTO static_data.contract_templates_commissions(contract_template_id, commission_id)
SELECT template_id, commission_id
FROM bkp.contract_commissions;

INSERT INTO static_data.contracts(contract_id, ticker, template_id, first_trading_date, expiration_date, synthetic_contract_type, synthetic_requires_bar_recalculation_at_rollover, external_contract_id, asset_id, description, default_datafeed_id)
SELECT contract_id, ticker, c.template_id, first_trading_dt, expiration_dt, synth_type,
       synth_requires_bar_recalculation, external_id, c.asset_id, c.description, t.default_datafeed_id
FROM bkp.contracts c
         INNER JOIN bkp.contract_templates t ON t.template_id = c.template_id
WHERE contract_id <> 10303;


INSERT INTO static_data.streams(stream_id, ticker, datafeed_id, enabled, contract_id)
SELECT stream_id, ticker, datafeed_id, enabled, contract_id
FROM bkp.streams
WHERE contract_id <> 10303;

INSERT INTO static_data.constant_value_streams(stream_id, value)
SELECT stream_id, value
FROM bkp.constant_value_streams;

INSERT INTO static_data.fx_conversion_contracts(contract_id)
SELECT contract_id
FROM bkp.fx_conversions;

insert into static_data.contract_templates(template_id, name, security_type, pl_calculator_type, asset_id, min_size, min_size_money, max_size, max_size_money, size_increment, tick_size, tick_value, price_quotation, settlement_currency_id, base_currency_id, quote_currency_id, default_datafeed_id, exchange_id, broker_id, days_in_year, description)
values (10303, 'TEST', 'Stock', 'Default', null, 1, null, 1, null, 1,
        0.00001, 0.00001, 1, 840, null, null,
        100, 115, 2, 252, '');

insert into static_data.contracts(contract_id, ticker, template_id, first_trading_date, expiration_date, synthetic_contract_type, synthetic_requires_bar_recalculation_at_rollover, external_contract_id, asset_id, description, default_datafeed_id) values
    (10303, 'TEST', 10303, null, null, null, null, null,
     null, null, 100);

insert into static_data.streams(stream_id, ticker, datafeed_id, enabled, contract_id) values
    (10303, 'TEST', 100, true, 10303);

COMMIT;

-- insert into infrastructure.locations(name) values ('local');
-- insert into infrastructure.as_instances(name, location_name) values ('AS-Tokyo', 'local');
-- insert into infrastructure.es_instances(name, location_name) values ('ES1', 'local');
-- insert into entities.trading_clients(account_id, execution_service, external_account_id, class_name, params, secret) 
-- values (
--     1000000,
--     'ES1',
--     null,
--     'Binance.Futures.USDM.TradingClient',
--     '{"Uri": "wss://fstream.binancefuture.com", "ApiKey": "RUtB5W9rdUvYEVvO1Z8CH4SRJXx9DNoqpyzgqVoBYNQdQBRVbJhWidzOBp8Sx3rF", "RestUri": "https://testnet.binancefuture.com", "ApiSecret": "nhYaKWOVltzmfP3F7R4zaWQoBYgUGNIgqrLIWcN8ugEsg4JJkko9mx5ycLEhmOD2"}',
--     ''
-- );
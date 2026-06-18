START TRANSACTION;

DROP FOREIGN TABLE IF EXISTS public.contracts;

CREATE FOREIGN TABLE IF NOT EXISTS public.contracts(
    contract_id bigint NOT NULL,
    ticker text NOT NULL COLLATE pg_catalog."default",
    type text NOT NULL COLLATE pg_catalog."default",
    parent_contract_id bigint,
    first_trading_dt timestamp with time zone,
    expiration_dt timestamp with time zone,
    rollover_dt timestamp with time zone,
    default_datafeed_id bigint,
    futures_no integer
    )
    SERVER main
    OPTIONS (table_name 'contracts');

ALTER FOREIGN TABLE public.contracts
    OWNER TO market_data;

GRANT ALL ON TABLE public.contracts TO market_data;

-- DELETE FROM "__EFMigrationsHistory"
-- WHERE "MigrationId" = '20250403151218_Synthetics';
-- 
-- COMMIT;
-- 
-- 

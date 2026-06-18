START TRANSACTION;

DROP FOREIGN TABLE IF EXISTS public.contracts;

CREATE FOREIGN TABLE IF NOT EXISTS public.contracts(
    contract_id bigint NOT NULL,
    default_datafeed_id bigint
)
SERVER main
OPTIONS (table_name 'contracts_view');

ALTER FOREIGN TABLE public.contracts
    OWNER TO market_data;

GRANT ALL ON TABLE public.contracts TO market_data;


DROP FUNCTION IF EXISTS public.get_contract_time_bau_end_num(bigint, timestamp with time zone, integer[], integer[], timestamp with time zone);

DROP FUNCTION IF EXISTS public.get_contract_time_bau_end_num_universal(bigint, timestamp with time zone, integer[], integer[], timestamp with time zone, integer);

DROP FUNCTION IF EXISTS public.get_contract_time_bau_start_end(bigint, timestamp with time zone, timestamp with time zone);

DROP FUNCTION IF EXISTS public.get_contract_time_bau_start_end_universal(bigint, timestamp with time zone, timestamp with time zone);

DROP FUNCTION IF EXISTS public.get_futures_time_bau_end_num(bigint, timestamp with time zone, integer[], integer[], timestamp with time zone, integer);

DROP FUNCTION IF EXISTS public.get_futures_time_bau_start_end(bigint, timestamp with time zone, timestamp with time zone, integer);

DROP FUNCTION IF EXISTS public.get_stream_time_bau_end_num(bigint, timestamp with time zone, integer[], integer[]);

CREATE OR REPLACE FUNCTION public.get_contract_time_bau_start_end(
    contract bigint,
    start_dt timestamp with time zone,
    end_dt timestamp with time zone)
    RETURNS TABLE(stream_id bigint, open_dt timestamp with time zone, close_dt timestamp with time zone, open double precision, high double precision, low double precision, close double precision, face_volume double precision, dollar_value double precision, trading_session_id integer)
    LANGUAGE 'sql'
    COST 100
    STABLE PARALLEL SAFE
    ROWS 1000

AS $BODY$
    SELECT
        bau.stream_id,
        bau.open_dt,
        bau.close_dt,
        bau.open,
        bau.high,
        bau.low,
        bau.close,
        bau.face_volume as volume,
        bau.dollar_value,
        bau.trading_session_id
    FROM
        public.time_bau bau
    WHERE
        bau.stream_id = (
            SELECT
                s.stream_id
            FROM
                public.contracts c
                    INNER JOIN public.streams s on s.contract_id = c.contract_id and s.datafeed_id = c.default_datafeed_id
            WHERE
                c.contract_id = contract
            LIMIT 1
        )
      AND bau.open_dt >= start_dt
      AND bau.open_dt < end_dt
    ORDER BY
        bau.close_dt
$BODY$;

ALTER FUNCTION public.get_contract_time_bau_start_end(bigint, timestamp with time zone, timestamp with time zone)
    OWNER TO market_data;


-- 
-- INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
-- VALUES ('20250403151218_Synthetics', '7.0.18');
-- 
-- COMMIT;
-- 
-- 

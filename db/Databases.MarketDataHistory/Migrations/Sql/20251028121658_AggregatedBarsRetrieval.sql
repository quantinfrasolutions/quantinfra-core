START TRANSACTION;

CREATE OR REPLACE FUNCTION public.get_aggregated_contract_candles_start_end(
    contract bigint,
    start_dt timestamp with time zone,
    end_dt timestamp with time zone,
    timeframe interval,
    "offset" interval,
    timezone text)
    RETURNS TABLE(stream_id bigint, open_dt timestamp with time zone, close_dt timestamp with time zone, open double precision, high double precision, low double precision, close double precision, face_volume double precision, dollar_value double precision, trading_session_id integer)
    LANGUAGE 'sql'
    COST 100
    STABLE PARALLEL SAFE
    ROWS 1000

AS $BODY$
SELECT
    stream_id,
    bucket AS open_dt,
    bucket + timeframe AS close_dt,
    open,
    high,
    low,
    close,
    volume,
    dollar_value,
    trading_session_id
FROM
    (
        SELECT
            bau.stream_id,
            time_bucket(timeframe, bau.open_dt, timezone, '2000-01-01', "offset") AS bucket,
            FIRST(bau.open, bau.open_dt) AS open,
            MAX(bau.high) AS high,
            MIN(bau.low) AS low,
            LAST(bau.close, bau.open_dt) AS close,
            SUM(bau.face_volume) AS volume,
            SUM(bau.dollar_value) AS dollar_value,
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
        GROUP BY
            bau.stream_id,
            bucket,
            bau.trading_session_id
        -- ORDER BY
        --     bau.open_dt
    ) sel
ORDER BY
    open_dt
$BODY$;

CREATE OR REPLACE FUNCTION public.get_aggregated_stream_candles_start_end(
    stream bigint,
    start_dt timestamp with time zone,
    end_dt timestamp with time zone,
    timeframe interval,
    "offset" interval,
    timezone text)
    RETURNS TABLE(stream_id bigint, open_dt timestamp with time zone, close_dt timestamp with time zone, open double precision, high double precision, low double precision, close double precision, face_volume double precision, dollar_value double precision, trading_session_id integer)
    LANGUAGE 'sql'
    COST 100
    STABLE PARALLEL SAFE
    ROWS 1000

AS $BODY$
SELECT
    stream_id,
    bucket AS open_dt,
    bucket + timeframe AS close_dt,
    open,
    high,
    low,
    close,
    volume,
    dollar_value,
    trading_session_id
FROM
    (
        SELECT
            bau.stream_id,
            time_bucket(timeframe, bau.open_dt, timezone, '2000-01-01', "offset") AS bucket,
            FIRST(bau.open, bau.open_dt) AS open,
            MAX(bau.high) AS high,
            MIN(bau.low) AS low,
            LAST(bau.close, bau.open_dt) AS close,
            SUM(bau.face_volume) AS volume,
            SUM(bau.dollar_value) AS dollar_value,
            bau.trading_session_id
        FROM
            public.time_bau bau
        WHERE
            bau.stream_id = stream
          AND bau.open_dt >= start_dt
          AND bau.open_dt < end_dt
        GROUP BY
            bau.stream_id,
            bucket,
            bau.trading_session_id
        -- ORDER BY
        --     bau.open_dt
    ) sel
ORDER BY
    open_dt
$BODY$;

-- INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
-- VALUES ('20251028121658_AggregatedBarsRetrieval', '9.0.6');
-- 
-- COMMIT;



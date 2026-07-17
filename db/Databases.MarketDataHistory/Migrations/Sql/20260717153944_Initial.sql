CREATE TABLE IF NOT EXISTS public.migrations_history (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK_migrations_history" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE SCHEMA data;

CREATE TABLE data.time_bau (
    trading_session_id integer,
    stream_id integer NOT NULL,
    open_dt timestamp with time zone NOT NULL,
    close_dt timestamp with time zone NOT NULL,
    open double precision NOT NULL,
    high double precision NOT NULL,
    low double precision NOT NULL,
    close double precision NOT NULL,
    face_volume double precision NOT NULL,
    dollar_value double precision NOT NULL
) WITH (
    tsdb.hypertable,
    tsdb.partition_column = 'open_dt',
    tsdb.segmentby = 'stream_id',
    tsdb.orderby = 'open_dt DESC'
);

CREATE INDEX ix_time_bau_symbol_time_time ON data.time_bau ("stream_id", "open_dt" ASC, "close_dt" ASC);

CREATE OR REPLACE FUNCTION data.get_stream_time_bau_start_end(
    stream integer,
    start_dt timestamp with time zone,
    end_dt timestamp with time zone)
    RETURNS TABLE(stream_id integer, open_dt timestamp with time zone, close_dt timestamp with time zone, open double precision, high double precision, low double precision, close double precision, face_volume double precision, dollar_value double precision, trading_session_id integer)
    LANGUAGE 'sql'
    COST 100
    STABLE PARALLEL SAFE
    ROWS 500000

AS $BODY$

SELECT
    stream_id,
    open_dt,
    close_dt,
    open,
    high,
    low,
    close,
    face_volume,
    dollar_value,
    trading_session_id
FROM
    data.time_bau b
WHERE
    b.stream_id = stream
  AND b.open_dt > start_dt
  AND b.close_dt <= end_dt

$BODY$;

CREATE OR REPLACE FUNCTION data.get_aggregated_stream_candles_start_end(
    stream integer,
    start_dt timestamp with time zone,
    end_dt timestamp with time zone,
    timeframe interval,
    "offset" interval,
    timezone text)
    RETURNS TABLE(stream_id integer, open_dt timestamp with time zone, close_dt timestamp with time zone, open double precision, high double precision, low double precision, close double precision, face_volume double precision, dollar_value double precision, trading_session_id integer)
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
            data.time_bau bau
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

-- INSERT INTO public.migrations_history ("MigrationId", "ProductVersion")
-- VALUES ('20260717153944_Initial', '9.0.6');
-- 
-- COMMIT;
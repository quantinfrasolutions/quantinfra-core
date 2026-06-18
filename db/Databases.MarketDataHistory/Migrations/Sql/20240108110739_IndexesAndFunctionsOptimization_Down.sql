START TRANSACTION;

DROP INDEX idx_time_bau_stream_id_open_dt ON time_bau (stream_id, open_dt);

DROP INDEX idx_time_bau_close_dt_open_dt ON time_bau (close_dt, open_dt);

CREATE INDEX idx_time_bau_close_dt ON time_bau (close_dt);

CREATE INDEX idx_time_bau_open_dt ON time_bau (open_dt);

CREATE INDEX idx_time_bau_stream_id ON time_bau (stream_id);


CREATE OR REPLACE FUNCTION public.get_contract_time_bau_end_num_universal(
	contract bigint,
	end_dt timestamp with time zone,
	num_bars INT,
	adj_finish_dt timestamp with time zone,
	use_futures_no int
)
RETURNS TABLE (
	stream_id BIGINT,
	open_dt timestamp with time zone, 
	close_dt timestamp with time zone, 
	open double precision, 
	high double precision, 
	low double precision, 
	close double precision, 
	face_volume double precision, 
	dollar_value double precision, 
	total_dividends double precision, 
	factor double precision,
	contract_id bigint,
	period bigint
)
LANGUAGE 'plpgsql'
COST 100
VOLATILE PARALLEL UNSAFE
AS $BODY$
DECLARE
contract_type TEXT;
	parent_contract_id BIGINT;
BEGIN
SELECT
    c.type
INTO
    contract_type
FROM
    public.contracts c
WHERE
        c.contract_id = contract
    LIMIT 1;

SELECT
    c.parent_contract_id
INTO
    parent_contract_id
FROM
    public.contracts c
WHERE
        c.contract_id = contract
    LIMIT 1;

IF (contract_type = 'Futures' AND parent_contract_id IS NULL) THEN
		RETURN QUERY
SELECT
    f.stream_id,
    f.open_dt,
    f.close_dt,
    f.open,
    f.high,
    f.low,
    f.close,
    f.face_volume,
    f.dollar_value,
    CAST (0.0 AS DOUBLE PRECISION) AS total_dividends,
    CAST (1.0 AS DOUBLE PRECISION) AS factor,
    f.contract_id,
    f.period
FROM
    public.get_futures_time_bau_end_num(contract, end_dt, num_bars, adj_finish_dt, use_futures_no) f;
END IF;

RETURN QUERY
SELECT
    f.stream_id,
    f.open_dt,
    f.close_dt,
    f.open,
    f.high,
    f.low,
    f.close,
    f.face_volume,
    f.dollar_value,
    f.total_dividends,
    f.factor,
    contract AS contract_id,
    CAST (0 AS BIGINT) AS period
FROM
    public.get_contract_time_bau_end_num(contract, end_dt, num_bars, adj_finish_dt) f;
END;
$BODY$


CREATE OR REPLACE FUNCTION public.get_contract_time_bau_end_num (
	contract bigint,
	end_dt timestamp with time zone,
	num_bars int,
	adj_finish_dt timestamp with time zone
)
RETURNS TABLE (
	stream_id BIGINT,
	open_dt timestamp with time zone, 
	close_dt timestamp with time zone, 
	open double precision, 
	high double precision, 
	low double precision, 
	close double precision, 
	face_volume double precision, 
	dollar_value double precision, 
	total_dividends double precision, 
	factor double precision
)
LANGUAGE 'sql'
VOLATILE PARALLEL UNSAFE
AS $BODY$
WITH bars AS (
	SELECT
		*
	FROM
	(
		SELECT
			bau.stream_id,
			bau.open_dt,
			bau.close_dt,
			bau.open,
			bau.high,
			bau.low,
			bau.close,
			bau.face_volume as volume,
			bau.dollar_value
		FROM 
			public.time_bau bau
		WHERE
			bau.stream_id = (
				SELECT 
					s.stream_id 
				FROM 
					public.contracts c
					INNER JOIN public.streams s on s.contract_id = c.contract_id and s.datafeed_id = c.primary_datafeed_id
				WHERE 
					c.contract_id = contract
				LIMIT 1
			)
			AND bau.close_dt <= end_dt		
		ORDER BY 
			bau.close_dt DESC
			LIMIT num_bars
	) sel
	ORDER BY
		close_dt ASC
), min_dt as (
	SELECT MIN(open_dt) as min_dt FROM bars LIMIT 1
), series as (
	SELECT
		*
	FROM
		generate_series((select min_dt from min_dt), adj_finish_dt, INTERVAL '1 minute') dt
), dividends AS (
	SELECT
		d.ex_dividend_date as dt,
		d.amount / COALESCE(MUL(s.factor), 1) as adjusted_amount
	FROM
		public.dividends d
		LEFT JOIN public.splits s ON s.dt > d.ex_dividend_date AND s.dt < adj_finish_dt
	WHERE
		d.contract_id = contract
		AND d.ex_dividend_date > (select min_dt from min_dt)
		AND d.ex_dividend_date < adj_finish_dt
	GROUP BY
		d.ex_dividend_date,
		d.amount
), normalized_dividends AS (
	SELECT
		s.dt,
		SUM(d.adjusted_amount) AS total_dividends
	FROM
		series s
		LEFT JOIN dividends d ON 
			d.dt > s.dt
	GROUP BY
		s.dt
), normalized_splits AS (
	SELECT
		s.dt,
		MUL(sp.factor) AS factor
	FROM
		series s
		LEFT JOIN public.splits sp ON 
			sp.contract_id = contract
			AND sp.dt > s.dt
			AND sp.dt < adj_finish_dt
	GROUP BY
		s.dt
), raw AS (
	SELECT
		bars.stream_id,
		bars.open_dt,
		bars.close_dt,
		bars.open,
		bars.high,
		bars.low,
		bars.close,
		bars.volume,
		bars.dollar_value,
		COALESCE(d.total_dividends, 0) AS total_dividends,
		COALESCE(s.factor, 1) AS factor
	FROM
		bars
		LEFT JOIN normalized_dividends d on d.dt = bars.open_dt
		LEFT JOIN normalized_splits s on s.dt = bars.open_dt
)
SELECT
    stream_id,
    open_dt,
    close_dt,
    open / factor - total_dividends AS open,
    high / factor - total_dividends AS high,
    low / factor - total_dividends AS low,
    close / factor - total_dividends AS close,
    volume * factor AS volume,
    dollar_value,
    total_dividends,
    factor
FROM
    raw
ORDER BY
    open_dt ASC

    $BODY$;

CREATE OR REPLACE FUNCTION public.get_futures_time_bau_start_end(
	contract bigint,
	start_dt timestamp with time zone,
	end_dt timestamp with time zone,
	use_futures_no integer DEFAULT 0
)
RETURNS TABLE(
	stream_id BIGINT,
	open_dt timestamp with time zone,
	close_dt timestamp with time zone,
	open double precision,
	high double precision,
	low double precision,
	close double precision,
	face_volume double precision,
	dollar_value double precision,
	contract_id bigint,
	period bigint
)
LANGUAGE 'sql'
COST 100
VOLATILE PARALLEL UNSAFE
AS $BODY$
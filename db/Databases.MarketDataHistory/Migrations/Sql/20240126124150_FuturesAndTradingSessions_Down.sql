START TRANSACTION;

ALTER TABLE time_bau DROP COLUMN trading_session_id;

DROP INDEX idx_time_bau_trading_session_id;

ALTER FOREIGN TABLE contracts RENAME COLUMN default_datafeed_id TO primary_datafeed_id;

-- from Initial
DROP FUNCTION public.get_stream_time_bau_start_end(
	stream bigint,
	start_dt timestamp with time zone,
	end_dt timestamp with time zone
);

CREATE OR REPLACE FUNCTION public.get_stream_time_bau_start_end(
	stream bigint,
	start_dt timestamp with time zone,
	end_dt timestamp with time zone
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
	dollar_value double precision
)
LANGUAGE 'sql'
VOLATILE PARALLEL UNSAFE
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
    dollar_value
FROM
    public.time_bau b
WHERE
    b.stream_id = stream
  AND b.open_dt > start_dt
  AND b.close_dt <= end_dt

$BODY$;


-- from Initial
DROP FUNCTION public.get_stream_time_bau_end_num(
	stream bigint,
	end_dt timestamp with time zone,
	trading_sessions int[],
 	num_bars int[]
);

CREATE OR REPLACE FUNCTION public.get_stream_time_bau_end_num(
	stream bigint,
	end_dt timestamp with time zone,
 	number_of_bars int
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
	dollar_value double precision
)
LANGUAGE 'sql'
VOLATILE PARALLEL UNSAFE
ROWS 500000
AS $BODY$

SELECT
    *
FROM
    (
        SELECT
            stream_id,
            open_dt,
            close_dt,
            open,
            high,
            low,
            close,
            face_volume,
            dollar_value
        FROM
            public.time_bau b
        WHERE
            b.stream_id = stream
          AND b.close_dt <= end_dt
        ORDER BY
            b.close_dt DESC
            LIMIT number_of_bars
    ) sel
ORDER BY
    close_dt ASC

$BODY$;


-- from Initial
DROP FUNCTION public.get_contract_time_bau_start_end(
	contract bigint,
	start_dt timestamp with time zone,
	end_dt timestamp with time zone
);

CREATE OR REPLACE FUNCTION public.get_contract_time_bau_start_end(
	contract bigint,
	start_dt timestamp with time zone,
	end_dt timestamp with time zone
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
		AND bau.open_dt >= start_dt
		AND bau.open_dt < end_dt
	ORDER BY 
		bau.close_dt
), series as (
	SELECT
		*
	FROM
		generate_series(start_dt, end_dt, INTERVAL '1 minute') dt
), dividends AS (
	SELECT
		d.ex_dividend_date as dt,
		d.amount / COALESCE(MUL(s.factor), 1) as adjusted_amount
	FROM
		public.dividends d
		LEFT JOIN public.splits s ON s.dt > d.ex_dividend_date AND s.dt < end_dt
	WHERE
		d.contract_id = contract
		AND d.ex_dividend_date > start_dt
		AND d.ex_dividend_date < end_dt
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
			AND sp.dt < end_dt
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

$BODY$;



-- from Initial
DROP FUNCTION public.get_futures_time_bau_start_end(
	contract bigint,
	start_dt timestamp with time zone,
	end_dt timestamp with time zone,
	use_futures_no integer DEFAULT 0
);

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
	
WITH prices AS
(
	SELECT
		*
	FROM
		(
			SELECT
				GREATEST(MIN(sel.dt) - INTERVAL '1 minute', start_dt) AS start,
				MAX(sel.dt) AS end,
				sel.contract_id,
				sel.period - use_futures_no AS period
			FROM
				(
					SELECT 
						series.dt,
						contracts.contract_id,
						contracts.period,
						ROW_NUMBER() OVER (PARTITION BY series.dt ORDER BY contracts.rollover_dt ASC) as contract_no
					FROM 
						(
							SELECT generate_series as dt FROM generate_series(start_dt, end_dt, INTERVAL '1 minute')
						) series
						LEFT JOIN
						(
							SELECT
								c.contract_id,
								c.first_trading_dt,
								c.rollover_dt,
								ROW_NUMBER() OVER () as period
							FROM
								public.contracts c
							WHERE
								c.parent_contract_id = contract
								AND c.rollover_dt >= start_dt
							ORDER BY
								rollover_dt
						) contracts ON contracts.rollover_dt >= series.dt AND contracts.first_trading_dt <= series.dt
				) sel
			WHERE
				sel.contract_no = use_futures_no + 1
			GROUP BY
				sel.contract_id, sel.period
		) periods
		INNER JOIN LATERAL 
			public.get_contract_time_bau_start_end(periods.contract_id, GREATEST(periods.start - INTERVAL '1 minute', start_dt), periods.end) ON true
), rollovers AS
(
	SELECT
		sel.period,
		sel.delta,
		SUM(sel.delta) OVER (ORDER BY sel.period DESC) as total_delta
	FROM
	(
		SELECT
			p.period,
			n.close - p.close as delta
		FROM
			prices p
			LEFT JOIN prices n on n.start = p.end
		WHERE
			p.close_dt = p.end
			AND n.open_dt < n.start
	) sel
), raw AS
(
	SELECT
		main.stream_id,
		main.open_dt as dt,
		main.close_dt,
		main.open + COALESCE(r.total_delta, 0) AS open,
		main.high + COALESCE(r.total_delta, 0) AS high,
		main.low + COALESCE(r.total_delta, 0) AS low,
		main.close + COALESCE(r.total_delta, 0) AS close,
		main.face_volume,
		main.dollar_value,
		main.contract_id,
		main.period
	FROM
		prices main
		LEFT JOIN rollovers r ON r.period = main.period
	WHERE
		main.open_dt >= main.start
)
SELECT
    stream_id,
    dt as open_dt,
    close_dt,
    open,
    high,
    low,
    close,
    face_volume,
    dollar_value,
    contract_id,
    period
FROM
    raw
ORDER BY
    open_dt;
$BODY$;



-- from IndexesAndFuturesOptimization
DROP FUNCTION public.get_contract_time_bau_end_num(
	contract bigint,
	end_dt timestamp with time zone,
	trading_sessions integer[],
	num_bars integer[],
	adj_finish_dt timestamp with time zone
);

CREATE OR REPLACE FUNCTION public.get_contract_time_bau_end_num(
	contract bigint,
	end_dt timestamp with time zone,
	num_bars integer,
	adj_finish_dt timestamp with time zone)
    RETURNS TABLE(stream_id bigint, open_dt timestamp with time zone, close_dt timestamp with time zone, open double precision, high double precision, low double precision, close double precision, face_volume double precision, dollar_value double precision, total_dividends double precision, factor double precision) 
    LANGUAGE 'sql'
    COST 10000
    STABLE PARALLEL SAFE
    ROWS 10000

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
			bau.open_dt DESC
			LIMIT num_bars
	) sel
	ORDER BY
		open_dt ASC
), min_dt as (
	SELECT MIN(open_dt) as min_dt FROM bars LIMIT 1
), series as (
	SELECT
		*
	FROM
		generate_series((select min_dt from min_dt), end_dt, INTERVAL '1 minute') dt
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




-- from Initial
DROP FUNCTION public.get_futures_time_bau_end_num(
	contract bigint,
	end_dt timestamp with time zone,
	trading_sessions integer[],
	num_bars integer[],
	num_bars INT,
	adj_finish_dt timestamp with time zone,
	use_futures_no integer DEFAULT 0
);

CREATE OR REPLACE FUNCTION public.get_futures_time_bau_end_num(
	contract bigint,
	end_dt timestamp with time zone,
	num_bars INT,
	adj_finish_dt timestamp with time zone,
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
	
WITH contracts AS (
	SELECT
		c.contract_id,
		c.first_trading_dt,
		c.rollover_dt,
		c.primary_datafeed_id,
		ROW_NUMBER() OVER () as period
	FROM
		public.contracts c
	WHERE
		c.parent_contract_id = contract
		AND c.rollover_dt >= end_dt
	ORDER BY
		rollover_dt
), start_date AS (
	SELECT
		MIN(open_dt) as start_dt
	FROM
	(
		SELECT
			b.open_dt
		FROM
			public.time_bau b
			INNER JOIN public.streams s ON s.stream_id = b.stream_id
			INNER JOIN contracts c ON s.contract_id = c.contract_id AND s.datafeed_id = c.primary_datafeed_id AND c.period = use_futures_no + 1
		WHERE
			b.open_dt < end_dt
		ORDER BY
			b.open_dt DESC
		LIMIT
			num_bars
	) sel
), prices AS
(
	SELECT
		*
	FROM
		(
			SELECT
				GREATEST(MIN(sel.dt), (SELECT start_dt from start_date LIMIT 1)) AS start,
				MAX(sel.dt) AS end,
				sel.contract_id,
				sel.period - use_futures_no AS period
			FROM
				(
					SELECT 
						series.dt,
						c.contract_id,
						c.period,
						ROW_NUMBER() OVER (PARTITION BY series.dt ORDER BY c.rollover_dt ASC) as contract_no
					FROM 
						(
							SELECT generate_series as dt FROM generate_series((SELECT start_dt from start_date LIMIT 1), end_dt, INTERVAL '1 minute')
						) series
						LEFT JOIN contracts c ON c.rollover_dt >= series.dt AND c.first_trading_dt <= series.dt
				) sel
			WHERE
				sel.contract_no = use_futures_no + 1
			GROUP BY
				sel.contract_id, sel.period
		) periods
		INNER JOIN LATERAL 
			public.get_contract_time_bau_start_end(periods.contract_id, GREATEST(periods.start, (SELECT start_dt from start_date LIMIT 1)), periods.end) ON true
), rollovers AS
(
	SELECT
		sel.period,
		sel.delta,
		SUM(sel.delta) OVER (ORDER BY sel.period DESC) as total_delta
	FROM
	(
		SELECT
			p.period,
			n.close - p.close as delta
		FROM
			prices p
			LEFT JOIN prices n on n.start = p.end
		WHERE
			p.close_dt = p.end
			AND n.open_dt < n.start
	) sel
), raw AS
(
	SELECT
		main.stream_id,
		main.open_dt as dt,
		main.close_dt,
		main.open + COALESCE(r.total_delta, 0) AS open,
		main.high + COALESCE(r.total_delta, 0) AS high,
		main.low + COALESCE(r.total_delta, 0) AS low,
		main.close + COALESCE(r.total_delta, 0) AS close,
		main.face_volume,
		main.dollar_value,
		main.contract_id,
		main.period
	FROM
		prices main
		LEFT JOIN rollovers r ON r.period = main.period
	WHERE
		main.open_dt >= main.start
		AND main.open_dt < end_dt
)
SELECT
    stream_id,
    dt as open_dt,
    close_dt,
    open,
    high,
    low,
    close,
    face_volume,
    dollar_value,
    contract_id,
    period
FROM
    raw
ORDER BY
    open_dt;
$BODY$;


-- From IndexesAndFunctionsOptimization
FUNCTION public.get_contract_time_bau_end_num_universal(
	contract bigint,
	end_dt timestamp with time zone,
	trading_sessions integer[],
	num_bars integer[],
	adj_finish_dt timestamp with time zone,
	use_futures_no integer
);

CREATE OR REPLACE FUNCTION public.get_contract_time_bau_end_num_universal(
	contract bigint,
	end_dt timestamp with time zone,
	num_bars integer,
	adj_finish_dt timestamp with time zone,
	use_futures_no integer)
    RETURNS TABLE(stream_id bigint, open_dt timestamp with time zone, close_dt timestamp with time zone, open double precision, high double precision, low double precision, close double precision, face_volume double precision, dollar_value double precision, total_dividends double precision, factor double precision, contract_id bigint, period bigint) 
    LANGUAGE 'plpgsql'
    COST 10000
    STABLE PARALLEL SAFE
    ROWS 10000

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
$BODY$;


-- From Initial
DROP FUNCTION public.get_contract_time_bau_start_end_universal(
	contract bigint,
	start_dt timestamp with time zone,
	end_dt timestamp with time zone,
	use_futures_no int
);

CREATE OR REPLACE FUNCTION public.get_contract_time_bau_start_end_universal(
	contract bigint,
	start_dt timestamp with time zone,
	end_dt timestamp with time zone,
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
    public.get_futures_time_bau_start_end(contract, start_dt, end_dt, use_futures_no) f;
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
    public.get_contract_time_bau_start_end(contract, start_dt, end_dt) f;
END;
$BODY$;
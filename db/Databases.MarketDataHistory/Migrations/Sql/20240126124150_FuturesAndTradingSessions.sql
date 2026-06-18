START TRANSACTION;

ALTER TABLE time_bau ADD trading_session_id integer NULL;

CREATE INDEX IF NOT EXISTS idx_time_bau_trading_session_id ON time_bau(trading_session_id, open_dt);

ALTER FOREIGN TABLE contracts RENAME COLUMN primary_datafeed_id TO default_datafeed_id;

-- Add trading_session_id to the results of get_stream_time_bau_start_end
-- Change stability
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
	dollar_value double precision,
	trading_session_id INTEGER
)
LANGUAGE 'sql'
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
    public.time_bau b
WHERE
    b.stream_id = stream
  AND b.open_dt > start_dt
  AND b.close_dt <= end_dt

$BODY$;


-- Add trading_session_id to the results of get_stream_time_bau_end_num
-- Change stability
DROP FUNCTION public.get_stream_time_bau_end_num(
	stream bigint,
	end_dt timestamp with time zone,
 	number_of_bars int
);

CREATE OR REPLACE FUNCTION public.get_stream_time_bau_end_num(
	stream bigint,
	end_dt timestamp with time zone,
	trading_sessions int[],
 	num_bars int[]
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
	trading_session_id integer
)
LANGUAGE 'sql'
STABLE PARALLEL SAFE
ROWS 500000
AS $BODY$
WITH nobs AS (
	SELECT
		ts,
		nob
	FROM
		UNNEST(num_bars) WITH ORDINALITY AS b(nob, nob_nr)
		LEFT JOIN UNNEST(trading_sessions) WITH ORDINALITY AS a(ts, ts_nr) ON ts_nr = nob_nr
), start_dts AS (
	-- Define the earliest open_dt per each of the requested trading sessions, given the number of bars
	-- required for that trading session
	SELECT
		n.ts,
		n.nob,
		(
			SELECT
				MIN(open_dt) AS dt
			FROM
				(
					SELECT
						bau.open_dt
					FROM
						time_bau bau
					WHERE
						bau.open_dt < end_dt
						AND bau.stream_id = stream
						AND (n.ts IS NULL OR bau.trading_session_id = n.ts)
					ORDER BY
						open_dt DESC
					LIMIT n.nob
				) sel
		) AS dt
	FROM 
		nobs n	
), start_dt AS (
	SELECT MIN(dt) as dt FROM start_dts
)
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
    public.time_bau b
WHERE
    b.stream_id = stream
	AND b.open_dt >= (SELECT dt FROM start_dt)
	AND b.open_dt < end_dt
ORDER BY
	open_dt ASC

$BODY$;


-- Add trading_session_id to the results of get_contract_time_bau_start_end
-- Change stability
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
	factor double precision,
	trading_session_id integer
)
LANGUAGE 'sql'
STABLE PARALLEL SAFE
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
		COALESCE(s.factor, 1) AS factor,
		bars.trading_session_id
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
    factor,
	trading_session_id
FROM
    raw

$BODY$;


-- Add trading_session_id to the results of get_futures_time_bau_start_end
-- Change stability
DROP FUNCTION public.get_futures_time_bau_start_end(
	contract bigint,
	start_dt timestamp with time zone,
	end_dt timestamp with time zone,
	use_futures_no integer
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
	period bigint,
	trading_session_id int
)
LANGUAGE 'sql'
COST 100
STABLE PARALLEL SAFE
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
		main.period,
		main.trading_session_id
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
    period,
	trading_session_id
FROM
    raw
ORDER BY
    open_dt;
$BODY$;

-- Change the signature (support for trading sessions) and add trading_session_id
-- to the resilts of get_contract_time_bau_end_num

DROP FUNCTION public.get_contract_time_bau_end_num(
	contract bigint,
	end_dt timestamp with time zone,	
	num_bars integer,
	adj_finish_dt timestamp with time zone);

CREATE OR REPLACE FUNCTION public.get_contract_time_bau_end_num(
	contract bigint,
	end_dt timestamp with time zone,
	trading_sessions integer[],
	num_bars integer[],
	adj_finish_dt timestamp with time zone)
    RETURNS TABLE(stream_id bigint, open_dt timestamp with time zone, close_dt timestamp with time zone, trading_session_id integer, open double precision, high double precision, low double precision, close double precision, face_volume double precision, dollar_value double precision, total_dividends double precision, factor double precision) 
    LANGUAGE 'sql'
    COST 10000
    STABLE PARALLEL SAFE
    ROWS 10000

AS $BODY$
WITH stream_id AS (
	SELECT 
		s.stream_id 
	FROM 
		public.contracts c
		INNER JOIN public.streams s on s.contract_id = c.contract_id and s.datafeed_id = c.default_datafeed_id
	WHERE 
		c.contract_id = contract
	LIMIT 1
),
nobs AS (
	SELECT
		ts,
		nob
	FROM
		UNNEST(num_bars) WITH ORDINALITY AS b(nob, nob_nr)
		LEFT JOIN UNNEST(trading_sessions) WITH ORDINALITY AS a(ts, ts_nr) ON ts_nr = nob_nr
), start_dts AS (
	-- Define the earliest open_dt per each of the requested trading sessions, given the number of bars
	-- required for that trading session
	SELECT
		n.ts,
		n.nob,
		(
			SELECT
				MIN(open_dt) AS dt
			FROM
				(
					SELECT
						bau.open_dt
					FROM
						time_bau bau
					WHERE
						bau.close_dt <= end_dt
						AND bau.stream_id = (select stream_id from stream_id)
						AND (n.ts IS NULL OR bau.trading_session_id = n.ts)
					ORDER BY
						open_dt DESC
					LIMIT n.nob
				) sel
		) AS dt
	FROM 
		nobs n	
), start_dt AS (
	SELECT MIN(dt) as dt FROM start_dts
),
bars AS (
	SELECT
		bau.stream_id,
		bau.open_dt,
		bau.close_dt,
		bau.trading_session_id,
		bau.open,
		bau.high,
		bau.low,
		bau.close,
		bau.face_volume as volume,
		bau.dollar_value
	FROM 
		public.time_bau bau
	WHERE
		bau.stream_id = (SELECT stream_id FROM stream_id)
		AND bau.open_dt >= (SELECT dt FROM start_dt)
		AND bau.open_dt < end_dt
	ORDER BY 
		bau.open_dt ASC
), series as (
	SELECT
		*
	FROM
		generate_series((select dt from start_dt), end_dt, INTERVAL '1 minute') dt
), dividends AS (
	SELECT
		d.ex_dividend_date as dt,
		d.amount / COALESCE(MUL(s.factor), 1) as adjusted_amount
	FROM
		public.dividends d
		LEFT JOIN public.splits s ON s.dt > d.ex_dividend_date AND s.dt < adj_finish_dt
	WHERE
		d.contract_id = contract
		AND d.ex_dividend_date > (select dt from start_dt)
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
		trading_session_id,
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
	trading_session_id,
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


-- Change the signature (support for trading sessions) and add trading_session_id
-- to the resilts of get_futures_time_bau_end_num

DROP FUNCTION public.get_futures_time_bau_end_num(
	contract bigint,
	end_dt timestamp with time zone,
	num_bars INT,
	adj_finish_dt timestamp with time zone,
	use_futures_no integer
);

CREATE OR REPLACE FUNCTION public.get_futures_time_bau_end_num(
	contract bigint,
	end_dt timestamp with time zone,
	trading_sessions integer[],
	num_bars integer[],
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
	period bigint,
	trading_session_id int
)
LANGUAGE 'sql'
COST 100
STABLE PARALLEL SAFE
AS $BODY$
	
WITH contracts AS (
	SELECT
		c.contract_id,
		c.first_trading_dt,
		c.rollover_dt,		
		ROW_NUMBER() OVER () as period
	FROM
		public.contracts c
		INNER JOIN streams s ON s.datafeed_id = c.default_datafeed_id
			AND s.contract_id = c.contract_id
	WHERE
		c.parent_contract_id = contract
		AND c.rollover_dt >= end_dt
	ORDER BY
		rollover_dt
), nobs AS (
	SELECT
		ts,
		nob
	FROM
		UNNEST(num_bars) WITH ORDINALITY AS b(nob, nob_nr)
		LEFT JOIN UNNEST(trading_sessions) WITH ORDINALITY AS a(ts, ts_nr) ON ts_nr = nob_nr
), start_dts AS (
	-- Define the earliest open_dt per each of the requested trading sessions, given the number of bars
	-- required for that trading session
	SELECT		
		n.ts,
		n.nob,
		(
			SELECT
				MIN(open_dt) AS dt
			FROM
				(
					SELECT
						bau.open_dt
					FROM
						time_bau bau
					WHERE
						bau.close_dt <= end_dt
						AND bau.stream_id IN (SELECT stream_id FROM contracts)
						AND (n.ts IS NULL OR bau.trading_session_id = n.ts)
					ORDER BY
						open_dt DESC
					LIMIT n.nob
				) sel
		) AS dt
	FROM 
		nobs n	
), start_date AS (
	SELECT MIN(dt) AS start_dt FROM start_dts
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
		main.period,
		main.trading_session_id
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
    period,
	trading_session_id
FROM
    raw
ORDER BY
    open_dt;
$BODY$;


-- Change the signature (support for trading sessions) and add trading_session_id
-- to the resilts of get_contract_time_bau_end_num_universal

DROP FUNCTION public.get_contract_time_bau_end_num_universal(
	contract bigint,
	end_dt timestamp with time zone,
	num_bars INT,
	adj_finish_dt timestamp with time zone,
	use_futures_no int
);

CREATE OR REPLACE FUNCTION public.get_contract_time_bau_end_num_universal(
	contract bigint,
	end_dt timestamp with time zone,
	trading_sessions integer[],
	num_bars integer[],
	adj_finish_dt timestamp with time zone,
	use_futures_no integer)
    RETURNS TABLE(stream_id bigint, open_dt timestamp with time zone, close_dt timestamp with time zone, trading_session_id integer, open double precision, high double precision, low double precision, close double precision, face_volume double precision, dollar_value double precision, total_dividends double precision, factor double precision, contract_id bigint, period bigint) 
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
    f.period,
	f.trading_session_id
FROM
    public.get_futures_time_bau_end_num(contract, end_dt, trading_sessions, num_bars, adj_finish_dt, use_futures_no) f;
END IF;

RETURN QUERY
SELECT
    f.stream_id,
    f.open_dt,
    f.close_dt,
	f.trading_session_id,
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
    public.get_contract_time_bau_end_num(contract, end_dt, trading_sessions, num_bars, adj_finish_dt) f;
END;
$BODY$;

-- Add trading_session_id to the resilts of get_contract_time_bau_start_end_universal
-- Change stability
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
	period bigint,
	trading_session_id int
)
LANGUAGE 'plpgsql'
COST 100
STABLE PARALLEL SAFE
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
    f.period,
	f.trading_session_id
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
    CAST (0 AS BIGINT) AS period,
	f.trading_session_id
FROM
    public.get_contract_time_bau_start_end(contract, start_dt, end_dt) f;
END;
$BODY$;

--INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
--VALUES ('20240126124150_FuturesAndTradingSessions', '7.0.11');

--COMMIT;
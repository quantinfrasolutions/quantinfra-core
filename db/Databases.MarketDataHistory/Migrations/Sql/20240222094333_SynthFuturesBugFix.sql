START TRANSACTION;

CREATE OR REPLACE FUNCTION public.get_contract_time_bau_start_end_universal(
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
    use_futures_no INT;
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
        c.parent_contract_id,
        c.futures_no
    INTO
        parent_contract_id,
        use_futures_no
    FROM
        public.contracts c
    WHERE
        c.contract_id = contract
    LIMIT 1;

    IF (contract_type = 'Futures') THEN
        IF (parent_contract_id IS NULL) THEN
            RAISE EXCEPTION 'Cannot retrieve data for a parent cotract';
        END IF;

        IF (use_futures_no IS NOT NULL) THEN
	        RETURN QUERY
				WITH prices AS (
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
						public.get_futures_time_bau_start_end(parent_contract_id, start_dt, end_dt, use_futures_no) f
				), current_stream_id AS (
					SELECT
						LAST(p.stream_id, p.open_dt) as stream_id
					FROM
						prices p
				)
				SELECT
					s.stream_id,
					p.open_dt,
					p.close_dt,
					p.open,
					p.high,
					p.low,
					p.close,
					p.face_volume,
					p.dollar_value,
					p.total_dividends,
					p.factor,
					p.contract_id,
					p.period,
					p.trading_session_id
				FROM
					prices p
					INNER JOIN current_stream_id s ON true;


        END IF;
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
								ROW_NUMBER() OVER (ORDER BY rollover_dt) as period
							FROM
								public.contracts c
							WHERE
								c.parent_contract_id = contract
								AND c.rollover_dt >= start_dt
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

-- INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
-- VALUES ('20240222094333_SynthFuturesBugFix', '7.0.11');

-- COMMIT;



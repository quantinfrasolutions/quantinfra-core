START TRANSACTION;

ALTER FOREIGN TABLE contracts DROP COLUMN futures_no;

DROP FUNCTION public.get_contract_time_bau_start_end_universal(
	contract bigint,
	start_dt timestamp with time zone,
	end_dt timestamp with time zone
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

-- DELETE FROM "__EFMigrationsHistory"
-- WHERE "MigrationId" = '20240216140750_SynthFuturesContracts';

-- COMMIT;



START TRANSACTION;

DROP FUNCTION public.get_contracts_last_price(
    contract_ids bigint[],
    dt timestamp with time zone
);

CREATE OR REPLACE FUNCTION public.get_contracts_last_price(
    contract_ids bigint[],
    dt timestamp with time zone
)
    RETURNS TABLE(stream_id bigint, contract_id bigint, actual_contract_id bigint, close double precision)
    LANGUAGE 'plpgsql'
    COST 10000
    STABLE PARALLEL SAFE
    ROWS 10000
AS
$BODY$
BEGIN
    RETURN QUERY
        WITH synth_futures AS (
            SELECT
                c.contract_id,
                act.contract_id AS actual_contract_id
            FROM
                contracts c
                    INNER JOIN
                (
                    SELECT
                        c.contract_id,
                        c.parent_contract_id,
                        c.first_trading_dt,
                        c.rollover_dt,
                        ROW_NUMBER() OVER (PARTITION BY parent_contract_id) as period
                    FROM
                        public.contracts c
                    WHERE
                        c.parent_contract_id IN (
                            SELECT
                                con.parent_contract_id
                            FROM
                                contracts con
                            WHERE
                                con.contract_id IN (SELECT * FROM UNNEST(contract_ids))
                              AND con.parent_contract_id IS NOT NULL
                              AND con.futures_no IS NOT NULL
                        )
                      AND c.rollover_dt >= dt
                    ORDER BY
                        rollover_dt
                ) act ON act.parent_contract_id = c.parent_contract_id AND period = c.futures_no + 1
            WHERE
                c.contract_id IN (SELECT * FROM UNNEST(contract_ids))
        ), final_contract_ids AS (
            SELECT
                con.contract_id,
                COALESCE(sf.actual_contract_id, con.contract_id) AS actual_contract_id
            FROM
                (SELECT * FROM UNNEST(contract_ids) AS contract_id) con
                    LEFT JOIN synth_futures sf ON sf.contract_id = con.contract_id
        ), streams_contracts AS (
            SELECT
                cids.contract_id,
                cids.actual_contract_id,
                s.stream_id
            FROM
                final_contract_ids cids
                    INNER JOIN contracts c ON c.contract_id = cids.actual_contract_id
                    INNER JOIN streams s ON c.contract_id = s.contract_id AND c.default_datafeed_id = s.datafeed_id
        )
        SELECT
            t.stream_id,
            sc.contract_id,
            sc.actual_contract_id,
            last(t.close, close_dt)
        FROM
            time_bau t
                INNER JOIN streams_contracts sc ON sc.stream_id = t.stream_id
        WHERE
            t.close_dt <= dt
          AND t.close_dt > dt - interval '1 day'
        GROUP BY
            t.stream_id, sc.contract_id, sc.actual_contract_id;
END;
$BODY$;


-- DELETE FROM "__EFMigrationsHistory"
-- WHERE "MigrationId" = '20250723165114_SyntheticsLastContractPrices';
-- 
-- COMMIT;
-- 
--
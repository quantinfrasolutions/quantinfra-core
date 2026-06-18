START TRANSACTION;

DROP FUNCTION public.get_contracts_last_price(
    contract_ids bigint[],
    dt timestamp with time zone
);

CREATE FUNCTION public.get_contracts_last_price(
    contract_ids bigint[],
    dt timestamp with time zone
)
    RETURNS TABLE(stream_id bigint, contract_id bigint, close double precision)
    LANGUAGE 'plpgsql'
    COST 10000
    STABLE PARALLEL SAFE
    ROWS 10000
AS
$BODY$
BEGIN
    RETURN QUERY
        SELECT
            s.stream_id,
            c.contract_id,
            last(t.close, t.close_dt)
        FROM
            contracts c
            INNER JOIN streams s ON c.contract_id = s.contract_id AND c.default_datafeed_id = s.datafeed_id
            INNER JOIN time_bau t ON 
                t.stream_id = s.stream_id 
                AND t.close_dt <= dt
                AND t.close_dt > dt - interval '1 day'
        WHERE
            c.contract_id IN (SELECT * FROM UNNEST(contract_ids))            
        GROUP BY
            s.stream_id, c.contract_id;
        
END;
$BODY$;

-- 
-- INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
-- VALUES ('20250723165114_SyntheticsLastContractPrices', '9.0.0');
-- 
-- COMMIT;
-- 
-- 

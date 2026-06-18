START TRANSACTION;

DROP FUNCTION IF EXISTS public.get_aggregated_contract_candles_start_end(bigint, timestamp with time zone, timestamp with time zone, interval, interval, text);
DROP FUNCTION IF EXISTS public.get_aggregated_stream_candles_start_end(bigint, timestamp with time zone, timestamp with time zone, interval, interval, text);


-- DELETE FROM "__EFMigrationsHistory"
-- WHERE "MigrationId" = '20251028121658_AggregatedBarsRetrieval';
-- 
-- COMMIT;
-- 
-- 

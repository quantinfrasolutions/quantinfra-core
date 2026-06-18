START TRANSACTION;

DROP FUNCTION public.get_contracts_last_price(contract_ids, dt);

-- DELETE FROM "__EFMigrationsHistory"
-- WHERE "MigrationId" = '20241009153404_LastContractPrices';

-- COMMIT;
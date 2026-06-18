START TRANSACTION;

CREATE INDEX idx_time_bau_close_dt ON time_bau (close_dt);

CREATE INDEX idx_time_bau_open_dt ON time_bau (open_dt);

CREATE INDEX idx_time_bau_stream_id ON time_bau (stream_id);

-- INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
-- VALUES ('20240103170709_TimeBauIndexes', '9.0.6');
-- 
-- COMMIT;
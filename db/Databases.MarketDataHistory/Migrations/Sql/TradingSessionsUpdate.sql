-- select * from trading_sessions_days where trading_session_id = 1 -- exchange_id = 1, main 09:30-16:00
-- select * from trading_sessions_days where trading_session_id = 3 -- exchange_id = 1, early 07:00-09:30
-- exchange_id == 1
UPDATE time_bau
SET 
	trading_session_id = CASE
		WHEN 
			extract(hour from open_dt AT TIME ZONE 'America/New_York') IN (7, 8)
			OR
			(extract(hour from open_dt AT TIME ZONE 'America/New_York') = 9 AND extract(minute from open_dt AT TIME ZONE 'America/New_York') < 30)
			THEN 3 -- early
		WHEN extract(hour from open_dt AT TIME ZONE 'America/New_York') BETWEEN 10 AND 15
			OR (extract(hour from open_dt AT TIME ZONE 'America/New_York') = 9 and extract(minute from open_dt AT TIME ZONE 'America/New_York') >= 30)
			THEN 1 -- main
		ELSE NULL
	END
WHERE
	stream_id in (1, 2, 7, 8, 9, 13, 14, 15, 17, 18, 19, 20, 22, 25); 
	
-- exchange_id = 2
-- select * from trading_sessions_days where trading_session_id = 2 -- exchange_id = 2, main 09:30-16:00
-- select * from trading_sessions_days where trading_session_id = 4 -- exchange_id = 2, pre-market 04:00-09:30
-- select * from trading_sessions_days where trading_session_id = 5 -- exchange_id = 5, after hours 16:00-20:00
UPDATE time_bau
SET 
	trading_session_id = CASE
		WHEN 
			extract(hour from open_dt AT TIME ZONE 'America/New_York') BETWEEN 4 AND 8
			OR
			(extract(hour from open_dt AT TIME ZONE 'America/New_York') = 9 AND extract(minute from open_dt AT TIME ZONE 'America/New_York') < 30)
			THEN 4 -- pre-market
		WHEN extract(hour from open_dt AT TIME ZONE 'America/New_York') BETWEEN 10 AND 15
			OR (extract(hour from open_dt AT TIME ZONE 'America/New_York') = 9 and extract(minute from open_dt AT TIME ZONE 'America/New_York') >= 30)
			THEN 2 -- main
		WHEN extract(hour from open_dt AT TIME ZONE 'America/New_York') BETWEEN 16 AND 19
			THEN 5 -- 
		ELSE NULL
	END
WHERE
	stream_id in (3, 4, 5, 6, 10, 11, 12, 16, 21, 23, 24, 26, 27);
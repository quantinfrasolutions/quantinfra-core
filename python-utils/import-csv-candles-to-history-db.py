import csv
from datetime import datetime, UTC

import psycopg

# Configuration
CONNECTION_STRING = "host= dbname= user= password="
CSV_FILE = ""
TRADING_SESSION_ID = 1051
STREAM_ID = 100000

with psycopg.connect(CONNECTION_STRING) as conn:
    with conn.cursor() as cur:
        with cur.copy(f"""
            COPY data.time_bau (
                trading_session_id,
                stream_id,
                open_dt,
                close_dt,
                open,
                high,
                low,
                close,
                face_volume,
                dollar_value                
            ) FROM STDIN
        """) as copy:

            with open(CSV_FILE, newline="") as f:
                reader = csv.reader(f)

                next(reader)  # Skip header

                for row in reader:
                    copy.write_row((
                        TRADING_SESSION_ID,
                        STREAM_ID,
                        datetime.fromtimestamp(int(row[0]) / 1000, UTC), # open_dt
                        datetime.fromtimestamp(int(row[0]) / 1000 + 60, UTC), # close_dt
                        float(row[1]), # open
                        float(row[2]), # high
                        float(row[3]), # low
                        float(row[4]), # close
                        float(row[5]), # volume                
                        float(row[7])  # quote volume
                    ))

print("Import complete.")
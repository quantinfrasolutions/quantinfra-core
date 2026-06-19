#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Network is used if you run the container inside Docker and need it to connect to the database running in another container.
# In other cases, it may be omitted.

docker run -d --rm \
  --name trading-engine \
  -p 8080:8080 \
  --network quantinfra-net \
  -v <PATH TO A DIRECTORY ON YOUR COMPUTER WHERE WAL FILES FILL BE STORED>/wal \
  -v <PATH TO A DIRECTORY ON YOUR COMPUTER THAT CONTAINS DLLS WITH CUSTOM STRATEGIES>:/strategies \
  --env-file "$SCRIPT_DIR/.env" \  
  ghcr.io/quantinfrasolutions/trading-engine:latest
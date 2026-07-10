#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Network is used if you run the container inside Docker and need it to connect to the database running in another container.
# In other cases, it may be omitted.

docker run -d --rm \
  --name tester-ui \
  -p 8080:8080 \
  --network quantinfra-net \
  -v <PATH TO A DIRECTORY ON YOUR COMPUTER THAT CONTAINS YOUR Strategies.dll>:/strategies \
  -v <PATH TO A DIRECTORY ON YOUR COMPUTER THAT WILL CONTAIN TEST RESULTS>:/results \
  -v <PATH TO A DIRECTORY ON YOUR COMPUTER THAT CONTAINS MARKET DATA>:/market-data \  
  --env-file "$SCRIPT_DIR/.env" \  
  ghcr.io/quantinfrasolutions/tester-ui:latest
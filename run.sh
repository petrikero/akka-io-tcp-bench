#!/usr/bin/env bash
# Builds the benchmark against each Akka build and runs every pattern, alternating builds, then prints medians.
#
#   ./run.sh                                  # 1.5.58 vs 1.6.0-beta4 (NuGet)
#   AKKA_SRC=~/akka.net ./run.sh              # also an Akka.dll built from a local akka.net checkout
#
# Env: REPS (3), WARMUP (10 s), MEASURE (30 s), RATE per connection (50/s), CONNS (100),
#      PATTERNS ("push sink reply"), SERVER_CPUS (0-7), CLIENT_CPUS (8-15).
set -euo pipefail
cd "$(dirname "$0")"
REPS=${REPS:-3}; WARMUP=${WARMUP:-10}; MEASURE=${MEASURE:-30}; RATE=${RATE:-50}; CONNS=${CONNS:-100}
PATTERNS=${PATTERNS:-"push sink reply"}; SERVER_CPUS=${SERVER_CPUS:-0-7}; CLIENT_CPUS=${CLIENT_CPUS:-8-15}
mkdir -p out logs

build() {  # build <name> <AkkaVersion> [Akka.dll]
  if [ ! -x out/$1/TcpBench ]; then
    rm -rf obj
    dotnet build src/TcpBench.csproj -c Release -p:AkkaVersion=$2 -o out/$1 > logs/build-$1.txt
    rm -rf src/obj
  fi
  if [ -n "${3:-}" ]; then cp "$3" out/$1/Akka.dll; fi
}
build v1.5.58 1.5.58
build v1.6.0-beta4 1.6.0-beta4
BUILDS="v1.5.58 v1.6.0-beta4"
if [ -n "${AKKA_SRC:-}" ]; then
  dotnet build "$AKKA_SRC/src/core/Akka/Akka.csproj" -c Release -o out/akka-local > logs/build-akka-local.txt
  rm -rf out/local; build local 1.6.0-beta4 out/akka-local/Akka.dll
  BUILDS="$BUILDS local"
fi

port=7800
: > logs/results.txt
for rep in $(seq "$REPS"); do for pat in $PATTERNS; do for b in $BUILDS; do
  port=$((port + 1)); args="pattern=$pat rate=$RATE conns=$CONNS warmup=$WARMUP measure=$MEASURE port=$port"
  env DOTNET_GCDynamicAdaptationMode=0 taskset -c "$SERVER_CPUS" out/$b/TcpBench server $args > logs/server.txt 2>&1 &
  sp=$!
  for _ in $(seq 100); do grep -q ready logs/server.txt 2>/dev/null && break; sleep 0.1; done
  taskset -c "$CLIENT_CPUS" out/v1.6.0-beta4/TcpBench client $args > logs/client.txt 2>&1
  wait $sp
  line="build=$b pattern=$pat rep=$rep $(grep -h 'RESULT\|CLIENT\|SERVERLAT' logs/server.txt logs/client.txt | sed 's/RESULT //;s/CLIENT //;s/SERVERLAT //' | tr '\n' ' ')"
  echo "$line" | tee -a logs/results.txt | cut -c1-160
done; done; done
python3 summarize.py logs/results.txt

#!/usr/bin/env bash
# Runs the TCP workloads on several Akka builds, paired: each repetition runs every workload on every build back to
# back, with the build order rotated per repetition. Then prints medians and the paired change versus BASELINE.
#
#   AKKA_BUILDS="dev=~/akka-dev pr=~/akka-pr" ./run.sh       # Akka.dll built from local akka.net checkouts
#
# Each AKKA_BUILDS entry is name=path to an akka.net checkout. 1.5.58 from NuGet is always included as a reference.
# Env: REPS (8), WARMUP (8 s), MEASURE (20 s), CONNS (100), WORKLOADS ("push:50 push:200 sink:50 reply:50", as
#      pattern:operations per second per connection), BASELINE (first AKKA_BUILDS entry),
#      SERVER_CPUS (0-7), CLIENT_CPUS (8-15).
set -euo pipefail
cd "$(dirname "$0")"
REPS=${REPS:-8}; WARMUP=${WARMUP:-8}; MEASURE=${MEASURE:-20}; CONNS=${CONNS:-100}
WORKLOADS=${WORKLOADS:-"push:50 push:200 sink:50 reply:50"}
SERVER_CPUS=${SERVER_CPUS:-0-7}; CLIENT_CPUS=${CLIENT_CPUS:-8-15}
: "${AKKA_BUILDS:?set AKKA_BUILDS to name=path entries}"
mkdir -p out logs

nuget() {  # nuget <AkkaVersion>
  if [ ! -x out/v$1/TcpBench ]; then
    dotnet build src/TcpBench.csproj -c Release -p:AkkaVersion=$1 -o out/v$1 > logs/build-v$1.txt
    rm -rf src/obj
  fi
}
nuget 1.5.58
nuget 1.6.0-beta4   # the client, and the base that local Akka.dll builds are dropped into

BUILDS="v1.5.58"
for entry in $AKKA_BUILDS; do
  name=${entry%%=*}; src=$(eval echo "${entry#*=}")
  dotnet build "$src/src/core/Akka/Akka.csproj" -c Release -o out/akka-$name > logs/build-akka-$name.txt
  rm -rf out/$name; cp -r out/v1.6.0-beta4 out/$name
  cp out/akka-$name/Akka.dll out/$name/
  # Akka on dev can depend on assemblies the 1.6.0-beta4 package did not.
  for f in out/akka-$name/*.dll; do [ -e out/$name/$(basename $f) ] || cp $f out/$name/; done
  BUILDS="$BUILDS $name"
done
BASELINE=${BASELINE:-$(echo $AKKA_BUILDS | cut -d' ' -f1 | cut -d= -f1)}

read -ra B <<< "$BUILDS"; n=${#B[@]}
port=7900
: > logs/results.txt
for rep in $(seq "$REPS"); do for w in $WORKLOADS; do pat=${w%%:*}; rate=${w#*:}
  for i in $(seq 0 $((n - 1))); do b=${B[$(( (i + rep) % n ))]}
    port=$((port + 1)); args="pattern=$pat rate=$rate conns=$CONNS warmup=$WARMUP measure=$MEASURE port=$port"
    env DOTNET_GCDynamicAdaptationMode=0 taskset -c "$SERVER_CPUS" out/$b/TcpBench server $args > logs/server.txt 2>&1 &
    sp=$!
    for _ in $(seq 100); do grep -q ready logs/server.txt 2>/dev/null && break; sleep 0.1; done
    taskset -c "$CLIENT_CPUS" out/v1.6.0-beta4/TcpBench client $args > logs/client.txt 2>&1
    wait $sp
    line="build=$b pattern=$pat rate=$rate rep=$rep load=$(cut -d' ' -f1 /proc/loadavg) $(grep -h 'RESULT\|CLIENT\|SERVERLAT' logs/server.txt logs/client.txt | sed 's/RESULT //;s/CLIENT //;s/SERVERLAT //' | tr '\n' ' ')"
    echo "$line" | tee -a logs/results.txt | cut -c1-160
done; done; done
python3 summarize.py logs/results.txt "$BASELINE"

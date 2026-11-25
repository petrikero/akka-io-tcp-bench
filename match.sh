#!/usr/bin/env bash
# ReceiveActor matching: an actor with 32 Receive<T> handlers, sent MILLIONS million messages that match handler k.
# Runs every build for every k, REPS times, with the build order alternating per repetition. Builds are directories
# under out/ that run.sh created.
#
#   ./match.sh v1.5.58 dev pr
#
# Env: REPS (3), KS ("0 16 31"), MILLIONS (10), CPUS (0-3).
set -euo pipefail
cd "$(dirname "$0")"
REPS=${REPS:-3}; KS=${KS:-"0 16 31"}; MILLIONS=${MILLIONS:-10}; CPUS=${CPUS:-0-3}
read -ra B <<< "$*"; n=${#B[@]}
mkdir -p logs; : > logs/match.txt
for rep in $(seq "$REPS"); do for k in $KS; do for i in $(seq 0 $((n - 1))); do b=${B[$(( (i + rep) % n ))]}
  echo "build=$b rep=$rep $(taskset -c "$CPUS" out/$b/TcpBench match k=$k millions=$MILLIONS | grep MATCH | sed 's/MATCH //')" \
    | tee -a logs/match.txt
done; done; done

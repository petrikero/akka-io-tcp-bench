"""Summarizes run.sh's results file: medians per build, and each build's paired change versus a baseline build.

    python3 summarize.py logs/results.txt [baseline]

The paired change is the mean over repetitions of (build / baseline) - 1, with a 95% t-interval.
"""
import math, re, statistics, sys
from collections import defaultdict

T95 = {1: 12.71, 2: 4.30, 3: 3.18, 4: 2.78, 5: 2.57, 6: 2.45, 7: 2.36, 8: 2.31, 9: 2.26, 10: 2.23}
METRICS = [("p50", "latency p50 (ms)"), ("p99", "latency p99 (ms)"), ("tp_items_per_op", "thread-pool items/op"),
           ("alloc_B_per_op", "allocated B/op"), ("cpu_us_per_op", "CPU µs/op")]

path = sys.argv[1]
baseline = sys.argv[2] if len(sys.argv) > 2 else "dev"
runs = defaultdict(lambda: defaultdict(dict))  # (pattern, rate) -> build -> rep -> fields
order = []
for line in open(path):
    kv = dict(re.findall(r"(\S+?)=(\S+)", line))
    if "ops" not in kv:
        continue
    runs[(kv["pattern"], kv["rate"])][kv["build"]][kv["rep"]] = kv
    if kv["build"] not in order:
        order.append(kv["build"])
builds = sorted(order, key=lambda b: (b != "v1.5.58", b != baseline, order.index(b)))


def paired(build, base, key):
    ratios = [float(build[r][key]) / float(base[r][key]) for r in build if r in base and float(base[r][key]) > 0]
    if len(ratios) < 2:
        return ""
    m = statistics.mean(ratios)
    h = T95.get(len(ratios) - 1, 1.96) * statistics.stdev(ratios) / math.sqrt(len(ratios))
    return f" {(m - 1) * 100:+.1f}% [{(m - h - 1) * 100:+.1f}, {(m + h - 1) * 100:+.1f}]"


for (pattern, rate), by_build in sorted(runs.items()):
    print(f"\n{pattern} at {rate}/s per connection")
    for key, label in METRICS:
        cells = []
        for b in builds:
            if b not in by_build:
                continue
            med = statistics.median(float(r[key]) for r in by_build[b].values())
            change = paired(by_build[b], by_build[baseline], key) if b != baseline and baseline in by_build else ""
            cells.append(f"{b}={med:.4g}{change}")
        print(f"  {label:<22} " + " | ".join(cells))

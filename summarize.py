"""Medians per (pattern, build) from run.sh's logs/results.txt."""
import re, statistics, sys
from collections import defaultdict

rows = defaultdict(list)
for line in open(sys.argv[1]):
    kv = dict(re.findall(r"(\S+?)=(\S+)", line))
    if "build" in kv and "ops" in kv:
        rows[(kv["pattern"], kv["build"])].append(kv)
cols = [("ops_per_s", "ops/s"), ("p50", "lat_p50_ms"), ("p99", "lat_p99_ms"), ("cpu_us_per_op", "cpu_us/op"),
        ("alloc_B_per_op", "alloc_B/op"), ("tp_items_per_op", "tp_items/op"), ("active_conns", "active_conns")]
print(f"{'pattern':<7} {'build':<14} {'n':>2} " + " ".join(f"{h:>13}" for _, h in cols))
for (pat, build), rs in sorted(rows.items()):
    cells = []
    for key, _ in cols:
        vals = [float(r[key]) for r in rs if key in r]
        cells.append(f"{statistics.median(vals):>13.4g}" if vals else f"{'-':>13}")
    print(f"{pat:<7} {build:<14} {len(rs):>2} " + " ".join(cells))

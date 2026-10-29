# Akka.IO TCP microbenchmark

This benchmark reproduces a performance regression in Akka.NET's Akka.IO TCP layer between **1.5.58** and **1.6.0-beta4**, and measures the fix proposed in [petrikero/akka.net#40](https://github.com/petrikero/akka.net/pull/40).

In 1.6, `TcpConnection` runs on `System.IO.Pipelines` with the default thread-pool schedulers. That adds a thread-pool hop to every write and every read, and `RequestPipeRead` allocates a new async state machine for every read. At moderate message rates this doubles one-way latency and adds about one thread-pool work item and thread wake-up per message. Akka's `TcpOperationsBenchmarks` saturates the connection, where the separate write pump coalesces writes, so it does not show the regression.

## What it measures

The server is an Akka.IO TCP listener with one handler actor per connection. The client is a set of raw sockets in a separate process, so the client side is the same for every Akka build. Every frame carries a `Stopwatch` timestamp, which is comparable across processes on the same machine, so latency is one-way and measured at the receiver.

| Pattern | Traffic | Latency measured |
|---|---|---|
| `push` | A server thread asks each handler to write one 128 B frame `rate` times per second. No acks. This is the write path. | server write to client receive |
| `sink` | The client sends 64 B frames `rate` times per second per connection, and the server only reads. This is the read path. | client send to handler receive |
| `reply` | Each 64 B request is answered with 4 separate 128 B `Tcp.Write`s. | request to last reply byte |

Over the measurement window, the server reports:
- CPU per operation;
- bytes allocated per operation;
- thread-pool work items per operation (`ThreadPool.CompletedWorkItemCount`);
- voluntary and involuntary context switches;
- the number of connections that were active.

## Running

You need the .NET 10 SDK, Linux (it reads `/proc` and uses `taskset`), and at least 16 cores for the default pinning.

```bash
./run.sh                                   # 1.5.58 vs 1.6.0-beta4 from NuGet
AKKA_SRC=~/src/akka.net ./run.sh           # also builds Akka.dll from a local akka.net checkout
```

The script builds each variant, runs every pattern for every build in alternating order, `REPS` times, and prints medians.

| Variable | Default | What it sets |
|---|---|---|
| `REPS` | 3 | repetitions of each pattern and build |
| `WARMUP` | 10 | warm-up seconds before measuring |
| `MEASURE` | 30 | measurement window in seconds |
| `RATE` | 50 | operations per second per connection |
| `CONNS` | 100 | number of connections |
| `PATTERNS` | `push sink reply` | patterns to run |
| `SERVER_CPUS` | `0-7` | cores the server is pinned to |
| `CLIENT_CPUS` | `8-15` | cores the client is pinned to |
| `SPIN` | `on` | `off` sets `DOTNET_ThreadPool_UnfairSemaphoreSpinLimit=0` |

You can also run the processes by hand:

```bash
dotnet build src/TcpBench.csproj -c Release -p:AkkaVersion=1.6.0-beta4 -o out/16
out/16/TcpBench server pattern=push rate=50 conns=100 warmup=10 measure=30 port=7700 &
out/16/TcpBench client pattern=push rate=50 conns=100 warmup=10 measure=30 port=7700
```

## Summary

At 5,000 operations/s over 100 connections (medians of 3 runs):

| | 1.5.58 | 1.6.0-beta4 | fix |
|---|---|---|---|
| push: one-way latency p50 | 0.090 ms | 0.173 ms | 0.100 ms |
| push: thread-pool work items per write | 2.02 | 3.02 | 2.02 |
| sink: allocated per 64 B read | 199 B | 352 B | 184–203 B |
| sink: thread-pool work items per read | 2.36 | 3.37 | 2.36 |
| reply: socket sends per 4-write burst | 1.85 | 1.00 | 0.98 |

CPU per operation was the same for all builds within run-to-run noise on this shared machine. In an application test with 100 clients over Akka.IO TCP, CPU per client message was 86.6 ms per 1,000 messages on 1.5.58, 91.9 on 1.6.0-beta4 and 85.9 with the fix, as the mean of 4 matched runs each. Thread-pool work items per message were 3.0, 4.0 and 3.0.

## Results

### Setup

- **Machine:** 24-core x86-64, Linux (WSL2) and .NET 10, with Server GC and `DOTNET_GCDynamicAdaptationMode=0`. The machine was shared with other workloads, at load averages of 15–45 (shown per table).
- **Pinning:** the server ran on cores 0–7 and the client on cores 8–15.
- **Load:** 100 connections at 50 operations/s each, 5,000 operations/s in total.
- **Windows:** 10 s warm-up, then a 20–30 s measurement window per run.
- **Run order:** builds were interleaved within every repetition.
- **Builds:**
  - `1.5.58` and `1.6.0-beta4` are the NuGet packages.
  - `fix` is an Akka.dll built from the fix branch: inline pipe schedulers, the deferred `FlushNow` for unacked writes, and one reused pipe reader per connection. The final matrix used the fix before its review clean-up. The cleaned-up version, the commit in the PR, was checked afterwards in single runs with matching results (see the end).
  - `inline-only` and `inline+coalesce` are intermediate steps of the fix, kept to show what each part contributes.
- **Spinning:** "spinning on" is the .NET default. "Spinning off" sets `DOTNET_ThreadPool_UnfairSemaphoreSpinLimit=0`, which removes idle spinning from the CPU and instruction counts.
- **Metrics:**
  - Instructions come from `perf stat` hardware counters on the server process over the measurement window.
  - Latency is one-way: `push` from server write to client receive, `sink` from client send to handler receive, and `reply` from request to the last reply byte.
  - Cells show the median, with the min–max range in parentheses.
- **Raw data:** every run's raw line is in `results/`.

### Final matrix: 1.5.58, 1.6.0-beta4, fix
Source: `results/final-matrix.txt`, with 20 s measurement windows.
#### push, thread-pool spinning on (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | fix |
|---|---|---|---|
| latency p50 (ms) | 0.090 (0.089–0.096) | 0.173 (0.166–0.279) | 0.100 (0.093–0.104) |
| latency p99 (ms) | 3.263 (0.451–3.568) | 0.615 (0.419–5.113) | 2.231 (2.073–2.896) |
| CPU µs/op | 149 (136–157) | 138 (132–159) | 145 (142–161) |
| user µs/op | 39 (33–41) | 41 (35–43) | 40 (36–47) |
| sys µs/op | 110 (103–117) | 97 (97–116) | 107 (105–114) |
| user instructions/op | 38063 (35110–42088) | 44234 (34930–45026) | 39966 (38735–41887) |
| kernel instructions/op | 82291 (70840–93711) | 92479 (63868–96382) | 76821 (73102–84456) |
| allocated B/op | 574 (572–583) | 343 (327–346) | 336 (336–338) |
| thread-pool items/op | 2.02 (2.02–2.02) | 3.02 (3.02–3.02) | 2.02 (2.02–2.02) |
| voluntary ctx switches/op | 0.77 (0.64–0.89) | 0.93 (0.54–0.95) | 0.70 (0.65–0.78) |
| involuntary ctx switches/op | 0.44 (0.01–0.91) | 0.03 (-0.24–0.03) | 0.86 (0.38–1.28) |
| machine load avg | 25.2 (11.9–33.4) | 23.3 (10.2–28.3) | 26.2 (13.8–28.2) |

#### sink, thread-pool spinning on (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | fix |
|---|---|---|---|
| latency p50 (ms) | 0.263 (0.261–0.321) | 0.281 (0.249–0.425) | 0.259 (0.217–0.426) |
| latency p99 (ms) | 3.421 (2.712–4.072) | 4.560 (3.552–8.556) | 5.443 (2.232–7.136) |
| CPU µs/op | 114 (113–114) | 108 (107–129) | 103 (94–134) |
| user µs/op | 21 (15–22) | 31 (27–33) | 29 (20–30) |
| sys µs/op | 92 (92–99) | 76 (74–102) | 74 (64–113) |
| user instructions/op | 14853 (14399–15232) | 21314 (17979–21462) | 20394 (15586–22714) |
| kernel instructions/op | 56736 (56410–59123) | 64313 (49651–65786) | 61729 (43748–70197) |
| allocated B/op | 188 (188–188) | 340 (340–341) | 181 (180–181) |
| thread-pool items/op | 2.36 (2.35–2.36) | 3.37 (3.30–3.38) | 2.36 (2.28–2.40) |
| voluntary ctx switches/op | 0.54 (0.52–0.59) | 0.66 (0.50–0.69) | 0.63 (0.42–0.75) |
| involuntary ctx switches/op | 1.63 (1.54–2.24) | 0.68 (0.48–1.69) | 0.53 (-0.23–2.23) |
| machine load avg | 27.6 (20.2–35.2) | 32.0 (27.0–33.6) | 33.4 (26.8–42.1) |

#### reply, thread-pool spinning on (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | fix |
|---|---|---|---|
| latency p50 (ms) | 0.082 (0.073–0.092) | 0.145 (0.129–0.255) | 0.046 (0.042–0.057) |
| latency p99 (ms) | 2.833 (0.373–4.232) | 2.164 (2.144–5.311) | 3.442 (1.934–3.466) |
| CPU µs/op | 150 (143–167) | 148 (144–154) | 150 (141–154) |
| user µs/op | 31 (28–47) | 45 (32–46) | 31 (29–34) |
| sys µs/op | 119 (114–120) | 108 (103–112) | 120 (110–120) |
| user instructions/op | 28896 (26668–38059) | 36022 (29030–37572) | 30845 (28196–30857) |
| kernel instructions/op | 85500 (75977–128231) | 91592 (56154–96054) | 70369 (57437–70936) |
| allocated B/op | 1581 (1581–1581) | 1141 (1141–1143) | 981 (981–982) |
| thread-pool items/op | 3.32 (3.28–3.33) | 5.35 (5.27–5.38) | 3.31 (3.28–3.36) |
| voluntary ctx switches/op | 0.50 (0.44–1.03) | 0.82 (0.49–0.89) | 0.60 (0.48–0.65) |
| involuntary ctx switches/op | 0.38 (0.08–1.61) | 0.58 (0.19–1.39) | 1.76 (1.50–1.97) |
| machine load avg | 41.0 (19.9–41.9) | 32.8 (19.0–46.3) | 34.3 (27.8–43.2) |

#### push, thread-pool spinning off (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | fix |
|---|---|---|---|
| latency p50 (ms) | 0.087 (0.069–0.101) | 0.226 (0.183–0.274) | 0.099 (0.095–0.107) |
| latency p99 (ms) | 3.278 (2.692–3.615) | 3.631 (3.546–6.529) | 3.413 (2.750–3.416) |
| CPU µs/op | 107 (102–114) | 110 (96–111) | 121 (115–127) |
| user µs/op | 26 (24–27) | 28 (26–30) | 32 (31–33) |
| sys µs/op | 83 (76–87) | 82 (68–84) | 90 (84–94) |
| user instructions/op | 27579 (26474–27697) | 30134 (27287–31155) | 31721 (30986–33826) |
| kernel instructions/op | 30383 (29625–31333) | 29854 (28807–30335) | 30695 (28948–31045) |
| allocated B/op | 570 (560–573) | 330 (314–336) | 331 (325–341) |
| thread-pool items/op | 2.02 (2.02–2.02) | 3.02 (3.01–3.02) | 2.02 (2.02–2.02) |
| voluntary ctx switches/op | 0.75 (0.72–0.84) | 0.75 (0.66–0.82) | 0.78 (0.78–0.88) |
| involuntary ctx switches/op | 0.17 (0.11–0.42) | 0.21 (0.09–0.40) | 0.23 (0.05–0.26) |
| machine load avg | 35.4 (23.9–36.7) | 31.7 (27.6–37.6) | 31.0 (25.0–34.7) |

#### sink, thread-pool spinning off (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | fix |
|---|---|---|---|
| latency p50 (ms) | 0.367 (0.356–0.384) | 0.499 (0.235–0.511) | 0.295 (0.269–0.379) |
| latency p99 (ms) | 4.600 (4.250–5.345) | 4.356 (3.973–5.575) | 3.863 (1.753–4.511) |
| CPU µs/op | 72 (70–74) | 76 (75–81) | 79 (77–84) |
| user µs/op | 17 (16–18) | 22 (20–22) | 20 (20–25) |
| sys µs/op | 54 (53–58) | 56 (53–60) | 59 (57–59) |
| user instructions/op | 10597 (10579–10623) | 14515 (14412–15169) | 14552 (14275–14608) |
| kernel instructions/op | 26035 (25822–26340) | 26373 (25849–26391) | 26144 (25998–26191) |
| allocated B/op | 188 (188–189) | 340 (340–340) | 181 (181–181) |
| thread-pool items/op | 2.35 (2.34–2.36) | 3.34 (3.32–3.36) | 2.38 (2.35–2.41) |
| voluntary ctx switches/op | 0.74 (0.69–0.74) | 0.70 (0.67–0.76) | 0.76 (0.72–0.83) |
| involuntary ctx switches/op | 0.32 (0.28–0.33) | 0.33 (0.09–0.39) | 0.20 (0.14–0.26) |
| machine load avg | 33.6 (32.3–43.4) | 40.9 (27.5–44.2) | 34.2 (25.8–42.2) |

#### reply, thread-pool spinning off (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | fix |
|---|---|---|---|
| latency p50 (ms) | 0.086 (0.081–0.093) | 0.140 (0.127–0.153) | 0.078 (0.076–0.079) |
| latency p99 (ms) | 1.530 (0.214–2.326) | 2.554 (0.263–2.648) | 0.982 (0.282–1.145) |
| CPU µs/op | 140 (122–148) | 129 (121–135) | 143 (141–145) |
| user µs/op | 30 (25–33) | 30 (30–33) | 38 (37–39) |
| sys µs/op | 109 (97–115) | 96 (91–105) | 105 (104–106) |
| user instructions/op | 24992 (23963–25627) | 27698 (26514–27794) | 27020 (26819–27469) |
| kernel instructions/op | 52056 (51308–53213) | 34829 (34692–35944) | 35378 (35103–35405) |
| allocated B/op | 1581 (1580–1581) | 1142 (1141–1142) | 981 (980–982) |
| thread-pool items/op | 3.34 (3.34–3.35) | 5.35 (5.35–5.35) | 3.36 (3.36–3.37) |
| voluntary ctx switches/op | 0.99 (0.87–1.00) | 0.88 (0.86–1.02) | 1.01 (1.00–1.02) |
| involuntary ctx switches/op | 0.06 (0.01–0.27) | 0.05 (0.01–0.22) | 0.05 (0.02–0.08) |
| machine load avg | 23.7 (22.5–34.6) | 21.2 (18.4–29.9) | 16.6 (15.3–25.9) |

#### Syscalls per operation (`strace -c -f`, 10 s, spinning off)

| pattern | build | send | futex | sched_yield |
|---|---|---|---|---|
| push | 1.5.58 | 1.00 | 1.44 | 0.13 |
| push | 1.6.0-beta4 | 0.99 | 2.39 | 0.20 |
| push | fix | 1.00 | 1.74 | 0.15 |
| reply | 1.5.58 | 1.85 | 0.49 | 0.06 |
| reply | 1.6.0-beta4 | 1.00 | 0.39 | 0.14 |
| reply | fix | 0.98 | 0.55 | 0.14 |
| sink | 1.5.58 | – | 1.39 | 0.23 |
| sink | 1.6.0-beta4 | – | 1.21 | 0.24 |
| sink | fix | – | 1.23 | 0.26 |

### Step 1: inline pipe schedulers only
Source: `results/matrix-inline-only.txt`, with 30 s windows. The inline schedulers remove the thread-pool hops but lose 1.6's write coalescing: the reply pattern makes 4.00 sends per 4-write burst, against 1.00 for stock 1.6. That is why the fix adds the deferred flush.
#### push, thread-pool spinning on (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | inline-only |
|---|---|---|---|
| latency p50 (ms) | 0.084 (0.082–0.085) | 0.166 (0.160–0.195) | 0.085 (0.078–0.091) |
| latency p99 (ms) | 0.458 (0.284–1.255) | 2.231 (1.649–4.305) | 2.139 (1.873–3.700) |
| CPU µs/op | 123 (115–131) | 130 (125–138) | 126 (116–135) |
| user µs/op | 33 (31–38) | 32 (24–33) | 31 (30–36) |
| sys µs/op | 91 (84–93) | 106 (92–106) | 89 (86–105) |
| user instructions/op | 38742 (38327–42286) | 34187 (33740–39497) | 35286 (34763–41668) |
| kernel instructions/op | 79524 (76075–94995) | 64869 (61917–81109) | 67186 (65160–88380) |
| allocated B/op | 584 (583–585) | 332 (328–338) | 336 (331–342) |
| thread-pool items/op | 2.02 (2.02–2.02) | 3.02 (3.01–3.02) | 2.02 (2.02–2.02) |
| voluntary ctx switches/op | 0.76 (0.73–0.91) | 0.58 (0.47–0.78) | 0.61 (0.60–0.87) |
| involuntary ctx switches/op | 0.04 (-0.11–0.08) | 1.26 (0.57–1.52) | 0.93 (0.18–1.18) |
| machine load avg | 18.4 (15.9–23.4) | 30.2 (19.3–31.4) | 31.0 (12.3–34.6) |

#### sink, thread-pool spinning on (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | inline-only |
|---|---|---|---|
| latency p50 (ms) | 0.203 (0.193–0.235) | 0.241 (0.188–0.261) | 0.269 (0.170–0.302) |
| latency p99 (ms) | 3.885 (0.775–4.391) | 2.302 (0.647–4.047) | 3.387 (0.416–4.193) |
| CPU µs/op | 92 (84–99) | 99 (86–101) | 108 (76–114) |
| user µs/op | 26 (25–26) | 27 (26–30) | 28 (27–29) |
| sys µs/op | 66 (58–74) | 72 (57–74) | 79 (48–86) |
| user instructions/op | 16868 (15785–16906) | 20023 (19545–22382) | 20692 (19289–22666) |
| kernel instructions/op | 65467 (61985–66980) | 63408 (61282–71726) | 66672 (61221–72055) |
| allocated B/op | 187 (187–187) | 339 (339–339) | 339 (339–339) |
| thread-pool items/op | 2.37 (2.35–2.39) | 3.37 (3.35–3.39) | 2.39 (2.36–2.40) |
| voluntary ctx switches/op | 0.69 (0.68–0.77) | 0.65 (0.64–0.77) | 0.70 (0.58–0.80) |
| involuntary ctx switches/op | 0.62 (0.30–0.72) | 0.80 (-0.09–0.88) | 0.73 (0.02–1.03) |
| machine load avg | 22.2 (14.5–26.9) | 20.0 (17.9–27.1) | 26.3 (11.5–30.3) |

#### reply, thread-pool spinning on (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | inline-only |
|---|---|---|---|
| latency p50 (ms) | 0.071 (0.069–0.082) | 0.126 (0.115–0.146) | 0.078 (0.070–0.091) |
| latency p99 (ms) | 1.046 (0.219–2.939) | 1.728 (0.472–3.564) | 1.581 (0.210–2.349) |
| CPU µs/op | 143 (123–148) | 137 (135–145) | 158 (148–183) |
| user µs/op | 31 (25–43) | 41 (36–41) | 47 (33–50) |
| sys µs/op | 101 (98–117) | 96 (94–108) | 115 (110–133) |
| user instructions/op | 29305 (29181–38026) | 37046 (32713–38893) | 47540 (41803–50832) |
| kernel instructions/op | 92702 (90582–128292) | 94558 (77076–104041) | 149845 (128608–164610) |
| allocated B/op | 1579 (1579–1579) | 1139 (1139–1140) | 1139 (1139–1139) |
| thread-pool items/op | 3.34 (3.33–3.35) | 5.36 (5.35–5.37) | 3.35 (3.35–3.36) |
| voluntary ctx switches/op | 0.65 (0.62–0.97) | 0.92 (0.64–0.95) | 0.90 (0.70–1.05) |
| involuntary ctx switches/op | 0.99 (-0.34–1.52) | 0.08 (-0.04–0.22) | 0.24 (-0.11–1.42) |
| machine load avg | 24.3 (18.0–35.5) | 21.0 (15.6–33.7) | 25.8 (18.3–33.1) |

#### push, thread-pool spinning off (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | inline-only |
|---|---|---|---|
| latency p50 (ms) | 0.089 (0.088–0.099) | 0.182 (0.149–0.240) | 0.088 (0.079–0.109) |
| latency p99 (ms) | 2.146 (0.652–3.485) | 2.889 (0.407–3.765) | 1.743 (0.236–2.517) |
| CPU µs/op | 102 (99–106) | 102 (100–119) | 106 (105–131) |
| user µs/op | 24 (21–24) | 24 (24–29) | 24 (24–31) |
| sys µs/op | 78 (78–82) | 78 (76–90) | 81 (81–100) |
| user instructions/op | 28405 (26087–29737) | 29764 (28979–32672) | 30473 (29339–32243) |
| kernel instructions/op | 30732 (30689–30814) | 29928 (27997–30136) | 29234 (28167–29336) |
| allocated B/op | 572 (558–580) | 332 (327–346) | 336 (329–346) |
| thread-pool items/op | 2.02 (2.02–2.02) | 3.02 (3.02–3.02) | 2.02 (2.02–2.02) |
| voluntary ctx switches/op | 0.82 (0.70–0.83) | 0.75 (0.67–0.77) | 0.84 (0.81–0.90) |
| involuntary ctx switches/op | 0.25 (0.22–0.37) | 0.19 (-0.04–0.23) | 0.14 (0.01–0.19) |
| machine load avg | 26.9 (26.1–43.6) | 24.4 (18.3–37.9) | 26.3 (12.4–29.2) |

#### sink, thread-pool spinning off (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | inline-only |
|---|---|---|---|
| latency p50 (ms) | 0.236 (0.229–0.370) | 0.243 (0.207–0.320) | 0.313 (0.273–0.466) |
| latency p99 (ms) | 2.873 (1.176–3.934) | 1.306 (1.252–9.912) | 2.930 (2.419–6.261) |
| CPU µs/op | 71 (65–87) | 75 (69–87) | 74 (72–83) |
| user µs/op | 17 (16–21) | 21 (21–24) | 21 (19–22) |
| sys µs/op | 53 (49–66) | 55 (48–63) | 52 (52–64) |
| user instructions/op | 9975 (9651–10281) | 14362 (13807–14456) | 13924 (13799–14314) |
| kernel instructions/op | 25909 (25451–25960) | 26270 (25364–26585) | 26144 (25835–26295) |
| allocated B/op | 187 (187–187) | 339 (338–339) | 339 (339–339) |
| thread-pool items/op | 2.36 (2.36–2.39) | 3.38 (3.31–3.40) | 2.37 (2.33–2.39) |
| voluntary ctx switches/op | 0.69 (0.69–0.75) | 0.78 (0.71–0.81) | 0.72 (0.69–0.76) |
| involuntary ctx switches/op | 0.16 (0.09–0.27) | 0.16 (0.13–0.20) | 0.22 (0.21–0.34) |
| machine load avg | 26.9 (15.1–29.7) | 24.7 (20.4–27.4) | 23.0 (20.4–36.2) |

#### reply, thread-pool spinning off (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | inline-only |
|---|---|---|---|
| latency p50 (ms) | 0.077 (0.073–0.078) | 0.128 (0.127–0.195) | 0.082 (0.081–0.097) |
| latency p99 (ms) | 2.117 (2.091–3.600) | 2.511 (1.208–3.861) | 0.779 (0.484–4.123) |
| CPU µs/op | 118 (112–124) | 110 (110–115) | 148 (141–164) |
| user µs/op | 25 (22–27) | 29 (26–30) | 34 (33–41) |
| sys µs/op | 93 (90–97) | 84 (81–86) | 114 (108–123) |
| user instructions/op | 24191 (22977–24578) | 26733 (25909–26916) | 36750 (36310–36837) |
| kernel instructions/op | 51012 (50885–51133) | 34663 (34241–34925) | 83443 (83238–83463) |
| allocated B/op | 1579 (1579–1579) | 1140 (1139–1140) | 1139 (1139–1139) |
| thread-pool items/op | 3.33 (3.29–3.34) | 5.36 (5.31–5.36) | 3.36 (3.33–3.36) |
| voluntary ctx switches/op | 0.83 (0.72–0.90) | 0.86 (0.70–0.87) | 0.97 (0.97–1.03) |
| involuntary ctx switches/op | 0.15 (0.09–0.31) | 0.12 (0.08–0.31) | 0.08 (0.05–0.08) |
| machine load avg | 23.8 (23.5–40.5) | 22.0 (21.6–41.5) | 20.6 (18.2–32.0) |

#### Syscalls per operation (`strace -c -f`, 10 s, spinning off)

| pattern | build | send | futex | sched_yield |
|---|---|---|---|---|
| push | 1.5.58 | 1.00 | 1.75 | 0.15 |
| push | 1.6.0-beta4 | 1.00 | 1.78 | 0.15 |
| push | inline-only | 1.01 | 1.67 | 0.14 |
| reply | 1.5.58 | 1.79 | 0.69 | 0.09 |
| reply | 1.6.0-beta4 | 0.98 | 0.77 | 0.11 |
| reply | inline-only | 4.00 | 0.30 | 0.03 |
| sink | 1.5.58 | – | 1.09 | 0.17 |
| sink | 1.6.0-beta4 | – | 1.01 | 0.18 |
| sink | inline-only | – | 0.94 | 0.18 |

### Step 2: inline schedulers + deferred flush
Source: `results/matrix-inline-coalesce.txt`, with 30 s windows. This step does not yet include the per-read allocation fix.
#### push, thread-pool spinning on (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | inline+coalesce |
|---|---|---|---|
| latency p50 (ms) | 0.090 (0.087–0.093) | 0.197 (0.193–0.213) | 0.092 (0.074–0.095) |
| latency p99 (ms) | 3.007 (1.760–3.182) | 3.626 (2.243–3.796) | 2.767 (1.272–5.678) |
| CPU µs/op | 134 (128–137) | 136 (127–139) | 140 (137–145) |
| user µs/op | 33 (30–35) | 33 (32–35) | 41 (30–42) |
| sys µs/op | 102 (95–104) | 101 (95–106) | 103 (99–107) |
| user instructions/op | 36144 (35610–37252) | 36826 (34538–38396) | 41539 (32852–41935) |
| kernel instructions/op | 74505 (72833–78892) | 71541 (65072–75448) | 83763 (54995–83898) |
| allocated B/op | 577 (574–577) | 332 (329–338) | 339 (322–339) |
| thread-pool items/op | 2.02 (2.02–2.02) | 3.02 (3.02–3.02) | 2.02 (2.02–2.02) |
| voluntary ctx switches/op | 0.67 (0.65–0.73) | 0.65 (0.57–0.65) | 0.77 (0.46–0.80) |
| involuntary ctx switches/op | 0.75 (0.65–0.77) | 0.62 (0.04–1.44) | 0.44 (0.22–1.64) |
| machine load avg | 26.2 (24.6–28.3) | 31.4 (23.7–36.0) | 28.8 (28.7–36.8) |

#### reply, thread-pool spinning on (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | inline+coalesce |
|---|---|---|---|
| latency p50 (ms) | 0.077 (0.055–0.088) | 0.153 (0.116–0.169) | 0.066 (0.061–0.076) |
| latency p99 (ms) | 2.266 (0.477–3.652) | 3.450 (0.331–5.078) | 4.064 (2.965–4.150) |
| CPU µs/op | 150 (139–159) | 145 (144–148) | 152 (149–168) |
| user µs/op | 40 (21–43) | 43 (36–44) | 44 (36–44) |
| sys µs/op | 118 (106–120) | 102 (101–112) | 116 (105–124) |
| user instructions/op | 33956 (24889–36622) | 34543 (31616–39695) | 34365 (31433–35769) |
| kernel instructions/op | 110911 (71063–123727) | 83185 (70256–107189) | 80469 (69721–87659) |
| allocated B/op | 1579 (1579–1580) | 1140 (1138–1141) | 1138 (1138–1139) |
| thread-pool items/op | 3.33 (3.29–3.33) | 5.34 (5.30–5.36) | 3.29 (3.29–3.32) |
| voluntary ctx switches/op | 0.84 (0.44–0.93) | 0.70 (0.63–0.99) | 0.73 (0.62–0.80) |
| involuntary ctx switches/op | 0.55 (-0.31–2.14) | 0.49 (0.03–1.01) | 0.89 (0.57–1.00) |
| machine load avg | 25.9 (24.0–48.3) | 23.8 (15.3–46.2) | 22.9 (19.5–44.6) |

#### push, thread-pool spinning off (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | inline+coalesce |
|---|---|---|---|
| latency p50 (ms) | 0.093 (0.064–0.095) | 0.180 (0.162–0.188) | 0.097 (0.071–0.099) |
| latency p99 (ms) | 1.932 (1.632–3.276) | 3.440 (2.344–3.579) | 2.874 (2.713–3.756) |
| CPU µs/op | 110 (88–119) | 93 (91–113) | 102 (83–112) |
| user µs/op | 24 (21–25) | 23 (22–26) | 26 (22–27) |
| sys µs/op | 86 (67–94) | 70 (70–87) | 75 (61–86) |
| user instructions/op | 29107 (23235–29802) | 28753 (26814–31907) | 29909 (29511–30080) |
| kernel instructions/op | 30324 (30033–31389) | 30830 (28471–30851) | 30858 (30080–31306) |
| allocated B/op | 576 (546–580) | 326 (312–342) | 322 (320–323) |
| thread-pool items/op | 2.02 (2.02–2.02) | 3.02 (3.02–3.02) | 2.02 (2.02–2.02) |
| voluntary ctx switches/op | 0.80 (0.66–0.81) | 0.76 (0.74–0.90) | 0.77 (0.74–0.80) |
| involuntary ctx switches/op | 0.16 (0.04–0.45) | 0.38 (0.03–0.47) | 0.37 (0.32–0.50) |
| machine load avg | 35.5 (19.5–36.8) | 28.9 (26.1–45.5) | 39.5 (32.8–44.2) |

#### reply, thread-pool spinning off (n=3 per build)
| | 1.5.58 | 1.6.0-beta4 | inline+coalesce |
|---|---|---|---|
| latency p50 (ms) | 0.082 (0.069–0.082) | 0.141 (0.138–0.160) | 0.072 (0.069–0.072) |
| latency p99 (ms) | 3.748 (3.173–3.875) | 2.594 (0.797–3.509) | 1.268 (0.663–2.954) |
| CPU µs/op | 105 (96–106) | 117 (111–132) | 125 (112–128) |
| user µs/op | 22 (21–24) | 30 (29–33) | 33 (31–34) |
| sys µs/op | 81 (75–83) | 88 (82–99) | 92 (81–94) |
| user instructions/op | 23128 (22723–23278) | 26443 (26037–26951) | 27507 (26808–27858) |
| kernel instructions/op | 52061 (51661–52830) | 34373 (34019–35995) | 34086 (34080–34606) |
| allocated B/op | 1579 (1579–1579) | 1139 (1139–1140) | 1139 (1139–1139) |
| thread-pool items/op | 3.31 (3.30–3.32) | 5.34 (5.33–5.36) | 3.35 (3.32–3.35) |
| voluntary ctx switches/op | 0.72 (0.70–0.74) | 0.87 (0.82–0.92) | 0.92 (0.78–0.95) |
| involuntary ctx switches/op | 0.35 (0.35–0.37) | 0.10 (0.04–0.36) | 0.05 (0.03–0.19) |
| machine load avg | 41.7 (35.8–46.1) | 33.3 (27.1–36.6) | 25.1 (20.5–42.3) |

### Read-path allocation profile (1.6.0-beta4, sink)

These come from `dotnet-trace --profile gc-verbose` over 12 s, using AllocationTick samples, which fire about every 100 KB:

| Share of sampled bytes | Type | Allocated in |
|---|---|---|
| 67.8% | `AsyncStateMachineBox<VoidTaskResult, TcpConnection+<ReadPipeAndSendAsync>d__71>` | `TcpConnection.HandlePipeRead` → `RequestPipeRead` |
| 14.1% | `TcpConnection+PipeReadCompleted` | `ReadPipeAndSendAsync` |
| 10.1% | `byte[]` (copy of the received bytes) | `ReadPipeAndSendAsync` |
| 5.5% | `Tcp.Received` | `TcpConnection.HandlePipeRead` |

On 1.5.58 the same pattern allocates `ByteString` (49%) and `byte[]` (42%) in `ByteString.CopyFrom`, 199 B per read in total. 1.6.0-beta4 allocates 352 B per read. With the fix, it is 184–203 B over 16 runs, and all 100 connections were active in every run.

### Check of the final fix commit (single runs, spinning on, 12 s windows)

| pattern | metric | 1.6.0-beta4 | fix (PR commit) |
|---|---|---|---|
| sink | allocated B/op | 345 | 184 |
| sink | thread-pool items/op | 3.29 | 2.33 |
| push | latency p50 (ms) | 0.243 | 0.101 |
| push | thread-pool items/op | 3.02 | 2.02 |
| reply | latency p50 (ms) | 0.172 | 0.060 |
| reply | thread-pool items/op | 5.34 | 3.33 |

### Reading the numbers

- **Latency and thread-pool items:**
  - Thread-pool work items per operation are identical across repetitions.
  - Latency medians are stable. The tails (p99) are noisy on a shared machine.
- **CPU:**
  - CPU µs per operation varies by 10–20% between runs at these load averages, so differences of that size between builds are not significant here.
  - Instruction counts with spinning off are the steadiest CPU metric.
- **Read path:** with the fix, user instructions per sink read (spinning off) stay at about 14.5k, against about 10.6k on 1.5.58. That is about 1 µs per read, and its cause is not identified.

## Notes

- Latency and thread-pool work items are stable between runs. CPU per operation is noisy on a busy machine, so use `REPS` of 3 or more and compare medians.
- `SPIN=off` removes the .NET thread pool's idle spinning, which otherwise adds CPU time that depends on load.

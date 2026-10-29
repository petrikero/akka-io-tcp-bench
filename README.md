# Akka.IO TCP microbenchmark

This benchmark reproduces a performance regression in Akka.NET's Akka.IO TCP layer between **1.5.58** and **1.6.0-beta4**, and measures the fix in [petrikero/akka.net#40](https://github.com/petrikero/akka.net/pull/40).

In 1.6, `TcpConnection` runs on `System.IO.Pipelines` with the default thread-pool schedulers. That adds a thread-pool hop to every write and every read, and `RequestPipeRead` allocates a new async state machine for every read. Akka's `TcpOperationsBenchmarks` saturates the connection, where the separate write pump coalesces writes, so it does not show the regression. This benchmark runs at a moderate, fixed rate, where most writes are isolated.

## What it measures

The server is an Akka.IO TCP listener with one handler actor per connection. The client is a set of raw sockets in a separate process, so the client side is the same for every Akka build. Every frame carries a `Stopwatch` timestamp, so latency is one-way and measured at the receiver.

| Pattern | Traffic |
|---|---|
| `push` | A server thread asks each handler to write one 128 B frame `rate` times per second, without acks. This is the write path. |
| `sink` | The client sends 64 B frames `rate` times per second per connection, and the server only reads. This is the read path. |
| `reply` | Each 64 B request is answered with 4 separate 128 B `Tcp.Write`s. Latency runs from the request to the last reply byte. |

The server reports CPU, allocated bytes and thread-pool work items (`ThreadPool.CompletedWorkItemCount`) per operation over the measurement window.

## Running

You need the .NET 10 SDK, Linux (`/proc`, `taskset`) and at least 16 cores for the default pinning.

```bash
./run.sh                                   # 1.5.58 vs 1.6.0-beta4 from NuGet
AKKA_SRC=~/src/akka.net ./run.sh           # also builds Akka.dll from a local akka.net checkout
```

The script builds each variant, runs every pattern for every build in alternating order, `REPS` times, and prints medians.

| Variable | Default | What it sets |
|---|---|---|
| `REPS` | 3 | repetitions of each pattern and build |
| `WARMUP` | 10 | warm-up seconds |
| `MEASURE` | 30 | measurement seconds |
| `RATE` | 50 | operations per second per connection |
| `CONNS` | 100 | number of connections |
| `PATTERNS` | `push sink reply` | patterns to run |
| `SERVER_CPUS` | `0-7` | cores the server is pinned to |
| `CLIENT_CPUS` | `8-15` | cores the client is pinned to |

## Results

### Setup

- **Machine:** 24-core x86-64 Linux (WSL2), .NET 10, Server GC.
- **Pinning:** the server ran on 8 cores and the client on 8 others.
- **Load:** 100 connections at 50 operations/s each, 5,000 operations/s in total.
- **Windows:** 10 s warm-up, then 20 s measured. Builds were interleaved within each repetition.
- **Builds:** `1.5.58` and `1.6.0-beta4` are the NuGet packages. `fix` is `Akka.dll` built from the fix branch.
- **Cells:** each cell is the median of 3 runs, with the min–max range in parentheses. The raw output of every run is in [`results/`](results/).

### push (write path)

| | 1.5.58 | 1.6.0-beta4 | fix |
|---|---|---|---|
| one-way latency p50 (ms) | 0.090 (0.089–0.096) | 0.173 (0.166–0.279) | 0.100 (0.093–0.104) |
| CPU per operation (µs) | 149 (136–157) | 138 (132–159) | 145 (142–161) |
| allocated per operation (B) | 574 (572–583) | 343 (327–346) | 336 (336–338) |
| thread-pool work items per operation | 2.02 (2.02–2.02) | 3.02 (3.02–3.02) | 2.02 (2.02–2.02) |

### sink (read path)

| | 1.5.58 | 1.6.0-beta4 | fix |
|---|---|---|---|
| one-way latency p50 (ms) | 0.263 (0.261–0.321) | 0.281 (0.249–0.425) | 0.259 (0.217–0.426) |
| CPU per operation (µs) | 114 (113–114) | 108 (107–129) | 103 (94–134) |
| allocated per operation (B) | 188 (188–188) | 340 (340–341) | 181 (180–181) |
| thread-pool work items per operation | 2.36 (2.35–2.36) | 3.37 (3.30–3.38) | 2.36 (2.28–2.40) |

### reply (4-write burst per request)

| | 1.5.58 | 1.6.0-beta4 | fix |
|---|---|---|---|
| one-way latency p50 (ms) | 0.082 (0.073–0.092) | 0.145 (0.129–0.255) | 0.046 (0.042–0.057) |
| CPU per operation (µs) | 150 (143–167) | 148 (144–154) | 150 (141–154) |
| allocated per operation (B) | 1581 (1581–1581) | 1141 (1141–1143) | 981 (981–982) |
| thread-pool work items per operation | 3.32 (3.28–3.33) | 5.35 (5.27–5.38) | 3.31 (3.28–3.36) |
| socket sends per request (`strace -c`) | 1.85 | 1.00 | 0.98 |
### Where the read allocation comes from

An EventPipe allocation profile of `sink` on 1.6.0-beta4 (`dotnet-trace --profile gc-verbose`):

| Share of sampled bytes | Type |
|---|---|
| 68% | `AsyncStateMachineBox<…, TcpConnection+<ReadPipeAndSendAsync>d__71>`, from `RequestPipeRead` |
| 14% | `TcpConnection+PipeReadCompleted` |
| 10% | `byte[]` (the copy of the received bytes) |
| 6% | `Tcp.Received` |

### Why the fix defers flushes

Inline pipe schedulers alone remove the hops, but they send every write separately. The `reply` pattern then makes 4.00 socket sends per request, against 1.00 on stock 1.6.0-beta4. The fix defers the flush of writes without an ack to a self-message, so a burst still leaves in one send (0.98 per request).

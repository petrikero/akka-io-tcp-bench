# Akka.IO TCP microbenchmark

This benchmark reproduces a latency and thread-pool regression in Akka.NET's Akka.IO TCP layer between **1.5.58** and **1.6.0-beta4**, and measures the fixes in [petrikero/akka.net#41](https://github.com/petrikero/akka.net/pull/41) (per-read allocation) and [#40](https://github.com/petrikero/akka.net/pull/40) (thread-pool hops, stacked on #41).

In 1.6, `TcpConnection` runs on `System.IO.Pipelines` with the default thread-pool schedulers. Every write and every read therefore crosses an extra thread-pool hop, and `RequestPipeRead` allocates a new async state machine for every read. Isolated writes take about twice as long to reach the socket, and each message costs one more thread-pool work item. CPU per message does not measurably change. Akka's `TcpOperationsBenchmarks` saturates the connection, where the separate write pump batches writes, so it does not show the regression. This benchmark runs at moderate, fixed rates, where most writes are isolated.

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

- **Machine:** 24-core x86-64 Linux (WSL2), .NET 10, Server GC. The machine was shared, at load averages of 15–40.
- **Pinning:** the server ran on 8 cores and the client on 8 others.
- **Load:** 100 connections at 50 or 200 operations/s each.
- **Windows:** 8 s warm-up, then 20 s measured.
- **Runs:** each of 8 repetitions ran every workload on all three builds back to back, in rotating order.
- **Builds:** `1.5.58` and `1.6.0-beta4` are the NuGet packages. `fix` is `Akka.dll` built from the PR branch.
- **Cells:** the build columns show medians. The ratio columns show the paired mean change with a 95% confidence interval. The raw output of every run is in [`results/paired-matrix.txt`](results/paired-matrix.txt).

#### push at 5,000 writes/s (write path)

| | 1.5.58 | 1.6.0-beta4 | fix | 1.6.0-beta4 vs 1.5.58 | fix vs 1.6.0-beta4 |
|---|---|---|---|---|---|
| one-way latency p50 (ms) | 0.094 | 0.191 | 0.092 | +101% [+78, +128] | -54% [-63, -41] |
| thread-pool work items per operation | 2.02 | 3.02 | 2.02 | +50% [+50, +50] | -33% [-33, -33] |
| allocated per operation (B) | 577 | 334 | 332 | -42% [-43, -41] | -0% [-2, +1] |
| CPU per operation (µs) | 137.8 | 136.1 | 135.7 | -4% [-9, +1] | +4% [-3, +11] |

#### push at 20,000 writes/s

| | 1.5.58 | 1.6.0-beta4 | fix | 1.6.0-beta4 vs 1.5.58 | fix vs 1.6.0-beta4 |
|---|---|---|---|---|---|
| one-way latency p50 (ms) | 0.076 | 0.258 | 0.069 | +252% [+201, +313] | -73% [-76, -69] |
| thread-pool work items per operation | 2.00 | 3.00 | 2.00 | +50% [+50, +50] | -34% [-34, -33] |
| allocated per operation (B) | 475 | 234 | 234 | -51% [-51, -50] | -0% [-1, +1] |
| CPU per operation (µs) | 60.4 | 49.5 | 57.2 | -11% [-23, +4] | +10% [-6, +30] |

#### sink at 5,000 reads/s (read path)

| | 1.5.58 | 1.6.0-beta4 | fix | 1.6.0-beta4 vs 1.5.58 | fix vs 1.6.0-beta4 |
|---|---|---|---|---|---|
| one-way latency p50 (ms) | 0.254 | 0.256 | 0.277 | +1% [-19, +26] | +6% [-15, +31] |
| thread-pool work items per operation | 2.36 | 3.37 | 2.38 | +43% [+42, +44] | -30% [-33, -28] |
| allocated per operation (B) | 188 | 341 | 181 | +81% [+80, +82] | -47% [-48, -46] |
| CPU per operation (µs) | 102.4 | 106.3 | 109.6 | +6% [-14, +30] | -9% [-28, +17] |

#### reply at 5,000 requests/s (4 writes per request)

| | 1.5.58 | 1.6.0-beta4 | fix | 1.6.0-beta4 vs 1.5.58 | fix vs 1.6.0-beta4 |
|---|---|---|---|---|---|
| one-way latency p50 (ms) | 0.085 | 0.144 | 0.072 | +63% [+41, +89] | -48% [-53, -44] |
| thread-pool work items per operation | 3.34 | 5.37 | 3.36 | +61% [+60, +61] | -37% [-38, -37] |
| allocated per operation (B) | 1581 | 1141 | 981 | -28% [-28, -28] | -14% [-14, -14] |
| CPU per operation (µs) | 153.4 | 138.4 | 151.7 | -9% [-15, -2] | +6% [-4, +16] |CPU per operation is not significantly different between the three builds in any workload. Latency, thread-pool work items and allocations are.

### Where the read allocation comes from

An EventPipe allocation profile of `sink` on 1.6.0-beta4 (`dotnet-trace --profile gc-verbose`):

| Share of sampled bytes | Type |
|---|---|
| 68% | `AsyncStateMachineBox<…, TcpConnection+<ReadPipeAndSendAsync>d__71>`, from `RequestPipeRead` |
| 14% | `TcpConnection+PipeReadCompleted` |
| 10% | `byte[]` (the copy of the received bytes) |
| 6% | `Tcp.Received` |

The allocation fix is [petrikero/akka.net#41](https://github.com/petrikero/akka.net/pull/41): one reused read continuation per connection instead of an async method per read. On its own it takes a 64 B read from 342 B to 214 B. The latency fix, [#40](https://github.com/petrikero/akka.net/pull/40), is stacked on top of it, and the `fix` columns above are for both together.

### The fix's trade-off at saturation

Akka's `TcpOperationsBenchmarks` sends bursts of unacked writes from 1–40 in-process clients as fast as possible. Time per message, fix vs 1.6.0-beta4:

| Clients | 1 | 3 | 5 | 7 | 10 | 20 | 30 | 40 |
|---|---|---|---|---|---|---|---|---|
| 10 B messages | −5% | −16% | −14% | −5% | −2% | +3% [−2, +8] | +2% [−3, +7] | +3% [−4, +12] |
| 100 B messages | −5% | −20% | −16% | −8% | −8% | +1% [−5, +8] | +4% [−3, +12] | **+15% [+12, +19]** |

The cells for 1–10 clients come from one round. The cells for 20–40 clients are paired means over 4 alternating rounds.

The fix sends from the actor's thread, which gives up some of the separate write pump's batching when many connections saturate the dispatcher. Writes still coalesce within one mailbox batch, because the flush is deferred to a self-message when more messages are queued.

## ReceiveActor handler matching

1.6.0-beta4 also replaced `ReceiveActor`'s compiled message matcher with a loop that calls `Type.IsAssignableFrom` on each registered handler in turn, so a message's cost grows with its handler's position. The `match` mode measures this: an actor with 32 `Receive<T>` handlers, sent messages that match handler `k`.

```bash
out/v1.6.0-beta4/TcpBench match k=31 millions=10
```

The table shows wall time per message, as the median of 3 runs, with the sender and the actor pinned to 4 cores. The raw output is in [`results/receive-actor-matching.txt`](results/receive-actor-matching.txt).

| Handler matched | 1.5.58 | 1.6.0-beta4 | 1.6.0-beta4 + dispatch cache |
|---|---|---|---|
| 1st | 53 ns | 58 ns | 52 ns |
| 17th | 53 ns | 98 ns | 58 ns |
| 32nd | 57 ns | 187 ns | 57 ns |

The dispatch cache, in [petrikero/akka.net#42](https://github.com/petrikero/akka.net/pull/42), remembers per message type which handlers can take it.

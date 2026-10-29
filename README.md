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

## Results

Full per-run tables (thread-pool spinning on and off, every metric with its min–max range), syscall counts, the allocation profile and the intermediate fix steps are in [RESULTS.md](RESULTS.md). The raw output of every run is in [`results/`](results/).

These ran on a 24-core Linux (WSL2) machine with .NET 10 and Server GC. Total load was 5,000 operations/s, from 100 connections at 50/s each. The server was pinned to 8 cores and the client to 8 others. Each cell is the median of 3 runs. The `futex` and `send` counts come from separate `strace -c -f` runs.

| | 1.5.58 | 1.6.0-beta4 | fix (PR #40) |
|---|---|---|---|
| push: one-way latency p50 | 0.090 ms | 0.173 ms | 0.100 ms |
| push: thread-pool work items per write | 2.02 | 3.02 | 2.02 |
| push: `futex` calls per write | 1.44 | 2.39 | 1.74 |
| sink: allocated per 64 B read | 199 B | 352 B | 184–203 B |
| sink: thread-pool work items per read | 2.36 | 3.37 | 2.36 |
| reply: latency p50 | 0.082 ms | 0.145 ms | 0.046 ms |
| reply: thread-pool work items per request | 3.32 | 5.35 | 3.31 |
| reply: socket sends per 4-write burst | 1.85 | 1.00 | 0.98 |

CPU per operation in this benchmark was the same for all three builds within run-to-run noise. The machine was shared, with load averages of 20–40. In an application test with 100 clients over Akka.IO TCP, CPU per client message was 86.6 ms per 1,000 messages on 1.5.58, 91.9 on 1.6.0-beta4 and 85.9 with the fix, as the mean of 4 matched runs each. Thread-pool work items per message were 3.0, 4.0 and 3.0.

### Where the read allocation comes from

An EventPipe allocation profile of `sink` on 1.6.0-beta4 shows where the read path allocates:

| Share of sampled bytes | Type | Allocated in |
|---|---|---|
| 68% | `AsyncStateMachineBox<…, TcpConnection+<ReadPipeAndSendAsync>d__71>` | `TcpConnection.HandlePipeRead` → `RequestPipeRead` |
| 14% | `PipeReadCompleted` | |
| 10% | `byte[]` (the copy of the received bytes) | |
| 6% | `Tcp.Received` | |

## Notes

- Latency and thread-pool work items are stable between runs. CPU per operation is noisy on a busy machine, so use `REPS` of 3 or more and compare medians.
- `SPIN=off` removes the .NET thread pool's idle spinning, which otherwise adds CPU time that depends on load.

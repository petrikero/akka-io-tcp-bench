# Akka.IO TCP and ReceiveActor microbenchmarks

This repository measures three open fixes for regressions in Akka.NET 1.6 relative to 1.5:

- [petrikero/akka.net#40](https://github.com/petrikero/akka.net/pull/40) removes the extra thread-pool hop that 1.6 added to every Akka.IO TCP write and read. This hop doubles the latency of isolated writes.
- [petrikero/akka.net#44](https://github.com/petrikero/akka.net/pull/44) reuses one `PipeReadCompleted` message per TCP connection instead of allocating one per read.
- [petrikero/akka.net#42](https://github.com/petrikero/akka.net/pull/42) caches `ReceiveActor` handler matches per message type. In 1.6, the cost of a message grows with its handler's position in registration order.

An earlier fix measured here, the per-read allocation fix [akkadotnet/akka.net#8874](https://github.com/akkadotnet/akka.net/pull/8874), is merged into Akka.NET `dev`. Its measurements against 1.6.0-beta4 are in this repository's git history.

## What it measures

**TCP workloads.** The server is an Akka.IO TCP listener with one handler actor per connection. The client is a set of raw sockets in a separate process, so the client side is the same for every Akka build. Every frame carries a `Stopwatch` timestamp, so latency is one-way and measured at the receiver.

| Pattern | Traffic |
|---|---|
| `push` | A server thread asks each handler to write one 128 B frame `rate` times per second, without acks. This is the write path. |
| `sink` | The client sends 64 B frames `rate` times per second per connection, and the server only reads. This is the read path. |
| `reply` | Each 64 B request is answered with 4 separate 128 B `Tcp.Write`s. Latency runs from the request to the last reply byte. |

The server reports CPU, allocated bytes and thread-pool work items (`ThreadPool.CompletedWorkItemCount`) per operation over the measurement window.

**ReceiveActor matching.** The `match` mode runs an actor with 32 `Receive<T>` handlers and sends it messages that match handler `k`. It reports wall time and process CPU time per message.

## Running

You need the .NET 10 SDK, Linux (`/proc`, `taskset`) and at least 16 cores for the default pinning.

```bash
# TCP workloads: 1.5.58 from NuGet, plus Akka.dll built from each local akka.net checkout
AKKA_BUILDS="dev=~/akka-dev pr40=~/akka-pr40 pr44=~/akka-pr44" ./run.sh

# ReceiveActor matching, on builds that run.sh created under out/
./match.sh v1.5.58 dev pr42
```

`run.sh` runs every workload on every build in each repetition, with the build order rotated per repetition. It then prints, for each workload, the median of each build and each build's paired change versus `BASELINE`. The paired change is the mean over repetitions of `build / baseline − 1`, with a 95% t-interval. `summarize.py` prints the same summary from a saved results file.

| Variable | Default | What it sets |
|---|---|---|
| `AKKA_BUILDS` | required | `name=path` entries for local akka.net checkouts |
| `BASELINE` | first `AKKA_BUILDS` entry | the build that the paired change compares against |
| `REPS` | 8 | repetitions |
| `WARMUP` | 8 | warm-up seconds per run |
| `MEASURE` | 20 | measured seconds per run |
| `CONNS` | 100 | number of connections |
| `WORKLOADS` | `push:50 push:200 sink:50 reply:50` | `pattern:rate` pairs, where rate is operations per second per connection |
| `SERVER_CPUS` | `0-7` | cores the server is pinned to |
| `CLIENT_CPUS` | `8-15` | cores the client is pinned to |

`match.sh` takes `REPS` (3), `KS` (`0 16 31`), `MILLIONS` (10) and `CPUS` (`0-3`).

Akka's own `TcpOperationsBenchmarks`, from an akka.net checkout, with the iteration counts used below:

```bash
cd src/benchmark/Akka.Benchmarks
dotnet run -c Release --framework net10.0 -- --filter '*TcpOperationsBenchmarks*' \
  --launchCount 1 --warmupCount 3 --iterationCount 10
```

## Results

### Setup

- **Machine:** an x86-64 Linux host (WSL2) with 24 logical and 12 physical cores, running .NET 10 with Server GC. Nothing else ran on it, and the load average stayed below 0.5.
- **Builds:** `dev` is Akka.NET `dev` at [`74ca5995f`](https://github.com/akkadotnet/akka.net/commit/74ca5995f). Each PR is a single commit on top of it: #40 at [`46a4851`](https://github.com/petrikero/akka.net/commit/46a485124), #44 at [`3416511`](https://github.com/petrikero/akka.net/commit/3416511ce) and #42 at [`1a252d2`](https://github.com/petrikero/akka.net/commit/1a252d267). `1.5.58` is the NuGet package, included as the reference for the regressions.
- **TCP workloads:** 100 connections, 8 s warm-up and 20 s measured per run, with 8 repetitions in rotating build order. 50 operations/s per connection is 5,000/s in total, and 200 is 20,000/s.
- **Cells:** the build columns show medians. The change columns show the paired mean change versus `dev`, with a 95% confidence interval.

### #40: thread-pool hops

| | 1.5.58 | `dev` | #40 | #40 vs `dev` |
|---|---|---|---|---|
| push 5k/s: one-way latency p50 | 0.062 ms | 0.116 ms | 0.062 ms | −46.8% [−50.6, −43.0] |
| push 20k/s: one-way latency p50 | 0.061 ms | 0.190 ms | 0.061 ms | −67.7% [−68.5, −66.8] |
| reply (4 writes per request): latency p50 | 0.062 ms | 0.087 ms | 0.051 ms | −41.2% [−41.6, −40.9] |
| sink: one-way latency p50 | 0.094 ms | 0.099 ms | 0.099 ms | −0.0% [−0.6, +0.6] |
| thread-pool work items per write (push 5k/s) | 2.02 | 3.02 | 2.02 | −33.1% |
| thread-pool work items per read (sink) | 2.38 | 3.39 | 2.40 | −29.4% |
| thread-pool work items per request (reply) | 3.28 | 5.37 | 3.37 | −37.3% |
| allocated per 64 B read (sink) | 188 B | 213 B | 181 B | −15.1% [−15.4, −14.9] |
| CPU per operation (push 5k/s) | 94.2 µs | 94.5 µs | 95.8 µs | +3.1% [−0.1, +6.2] |
| CPU per operation (push 20k/s) | 59.6 µs | 55.3 µs | 56.8 µs | +4.7% [+0.6, +8.8] |
| CPU per operation (sink) | 46.1 µs | 48.2 µs | 48.5 µs | +0.5% [−1.2, +2.1] |
| CPU per operation (reply) | 118.1 µs | 102.8 µs | 105.4 µs | +2.9% [+1.5, +4.3] |

#40 brings latency and thread-pool work back to the 1.5.58 level. CPU per operation rises by 3–5% against `dev` on the write-heavy workloads, and stays below 1.5.58 in push 20k/s and reply.

**At saturation.** Akka's `TcpOperationsBenchmarks` sends bursts of unacked writes from 1–40 in-process clients as fast as possible. #40 sends from the actor's thread, so it gives up the separate write pump's batching across mailbox batches. The table shows time per message over 4 rounds in alternating build order, each with 1 launch, 3 warm-up and 10 measured iterations per case.

| Clients | 10 B: `dev` | 10 B: #40 | 10 B: change | 100 B: `dev` | 100 B: #40 | 100 B: change |
|---|---|---|---|---|---|---|
| 1 | 8,328 ns | 8,109 ns | −2.2% [−3.3, −1.1] | 8,740 ns | 8,296 ns | −5.1% [−5.9, −4.3] |
| 3 | 2,234 ns | 1,936 ns | −13.0% [−15.1, −10.9] | 2,429 ns | 2,062 ns | −15.1% [−16.2, −14.1] |
| 5 | 1,435 ns | 1,226 ns | −14.1% [−17.6, −10.7] | 1,583 ns | 1,306 ns | −15.8% [−22.0, −9.7] |
| 7 | 1,075 ns | 922 ns | −13.9% [−15.2, −12.5] | 1,165 ns | 1,001 ns | −13.0% [−18.9, −7.1] |
| 10 | 756 ns | 708 ns | −5.3% [−12.5, +2.0] | 814 ns | 751 ns | −8.1% [−9.6, −6.5] |
| 20 | 512 ns | 489 ns | −4.3% [−5.1, −3.4] | 570 ns | 538 ns | −6.1% [−7.3, −4.9] |
| 30 | 449 ns | 414 ns | −6.7% [−10.2, −3.2] | 497 ns | 460 ns | −5.2% [−14.3, +3.8] |
| 40 | 412 ns | 375 ns | −9.3% [−12.5, −6.0] | 442 ns | 420 ns | −5.9% [−10.0, −1.9] |

#40 is faster than `dev` in every case. An earlier measurement against 1.6.0-beta4, on a host at load averages of 15–40, showed 15% more time per message at 40 clients with 100 B messages. That does not reproduce against `dev` on an idle host.

### #44: read message reuse

| Allocated per operation | `dev` | #44 | #44 vs `dev` |
|---|---|---|---|
| sink: one 64 B read | 213 B | 165 B | −22.7% [−22.8, −22.5] |
| reply: one 64 B read and four 128 B writes | 1013 B | 965 B | −4.8% [−4.8, −4.7] |

Latency, thread-pool work items and CPU per operation do not change significantly in any workload.

### #42: ReceiveActor handler matching

Wall time per message, as the median of 3 runs of 10 million messages, with the sender and the actor pinned to 4 cores:

| Handler matched | 1.5.58 | `dev` | #42 |
|---|---|---|---|
| 1st | 58 ns | 55 ns | 63 ns |
| 17th | 61 ns | 109 ns | 64 ns |
| 32nd | 65 ns | 165 ns | 63 ns |

With #42, the cost no longer depends on where the handler sits in the list. In return, a message whose handler is first pays for a dictionary lookup on the message type. In 8 further paired runs at the 1st handler, wall time per message rose from 59.4 ns to 64.9 ns (+11.9% [+5.9, +17.8]), and process CPU per message by 5.0% [+2.0, +8.0].

### Raw data

- [`results/tcp-paired.txt`](results/tcp-paired.txt): every TCP workload run. The builds are named after their branches: `akka-io-tcp-inline-pipes` is #40 and `akka-io-tcp-read-message-reuse` is #44.
- [`results/tcp-operations-benchmarks/`](results/tcp-operations-benchmarks/): the BenchmarkDotNet report of each round.
- [`results/receive-actor-matching.txt`](results/receive-actor-matching.txt) and [`results/receive-actor-matching-k0-paired.txt`](results/receive-actor-matching-k0-paired.txt): the `match` runs, where `receive-dispatch-cache` is #42. The first file's runs used a fixed build order, before `match.sh` rotated it. The second alternated the order.

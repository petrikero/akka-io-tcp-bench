// Akka.IO TCP microbenchmark (server = Akka.IO, client = raw sockets in another process).
// Patterns:
//   reply: client sends 64-byte requests at a fixed rate; the handler answers each with `replies` separate Tcp.Writes.
//   push:  a server thread tells each handler to push one write `rate` times/s; the client only reads. (write path)
//   sink:  client sends 64-byte messages at a fixed rate; the server only reads. (read path)
// Every frame carries a Stopwatch timestamp (CLOCK_MONOTONIC, comparable across processes), so one-way latency is
// measured at the receiving side: by the client for reply/push, by the server handler for sink.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Akka.Actor;
using Akka.IO;

static class Program
{
    const int RequestSize = 64;

    static int Main(string[] args)
    {
        var mode = args[0];
        var o = args.Skip(1).Select(a => a.Split('=', 2)).ToDictionary(a => a[0], a => a[1]);
        int Opt(string k, int d) => o.TryGetValue(k, out var v) ? int.Parse(v) : d;
        var cfg = new Cfg(o.TryGetValue("pattern", out var p) ? p : "reply", Opt("port", 7700), Opt("conns", 100), Opt("rate", 20),
            Opt("replies", 4), Opt("replySize", 128), Opt("warmup", 10), Opt("measure", 30));
        if (mode == "match") return MatchBench.Run(Opt("k", 0), Opt("millions", 20));
        return mode == "server" ? Server.Run(cfg) : Client.Run(cfg);
    }

    public record Cfg(string Pattern, int Port, int Conns, int Rate, int Replies, int ReplySize, int Warmup, int Measure)
    {
        public int FrameSize => Pattern == "sink" ? RequestSize : ReplySize;
    }

    // Lock-free latency histogram: 1 µs buckets up to 100 ms.
    public sealed class Hist
    {
        readonly long[] _b = new long[100_001];
        public volatile bool On;
        public void Add(long ticks)
        {
            if (!On) return;
            long us = ticks * 1_000_000 / Stopwatch.Frequency;
            Interlocked.Increment(ref _b[Math.Clamp(us, 0, _b.Length - 1)]);
        }
        public string Summary()
        {
            long n = _b.Sum(); if (n == 0) return "samples=0";
            double mean = 0; for (int i = 0; i < _b.Length; i++) mean += (double)i * _b[i] / n;
            string Q(double q) { long t = (long)(q * n), c = 0; for (int i = 0; i < _b.Length; i++) { c += _b[i]; if (c > t) return (i / 1000.0).ToString("F3"); } return "inf"; }
            return $"samples={n} mean_ms={mean / 1000:F3} p50={Q(0.5)} p90={Q(0.9)} p99={Q(0.99)} p999={Q(0.999)}";
        }
    }

    static void Stamp(byte[] buf, int offset) => BitConverter.TryWriteBytes(buf.AsSpan(offset), Stopwatch.GetTimestamp());

    public static class Server
    {
        public static long Ops;
        public static readonly long[] PerConn = new long[4096];
        public static int NextConn = -1;
        public static readonly Hist Latency = new();
        public static readonly List<IActorRef> Handlers = new();

        public static int Run(Cfg cfg)
        {
            var sys = ActorSystem.Create("bench", "akka.loglevel = WARNING\nakka.stdout-loglevel = WARNING");
            sys.ActorOf(Props.Create(() => new Listener(cfg)), "listener");
            Console.WriteLine($"akka={typeof(ActorSystem).Assembly.GetName().Version} pattern={cfg.Pattern} ready");
            var stop = false;
            if (cfg.Pattern == "push")
                new Thread(() => Pusher(cfg, ref stop)) { IsBackground = true }.Start();
            Thread.Sleep(cfg.Warmup * 1000);
            var a = Snapshot.Take(); Latency.On = true; var perConnA = (long[])PerConn.Clone();
            Thread.Sleep(cfg.Measure * 1000);
            Latency.On = false; var b = Snapshot.Take();
            int conns = Volatile.Read(ref NextConn) + 1, active = Enumerable.Range(0, conns).Count(i => PerConn[i] > perConnA[i]);
            double secs = (b.Wall - a.Wall).TotalSeconds, ops = b.Ops - a.Ops;
            double cpu = (b.User + b.Sys - a.User - a.Sys).TotalMilliseconds;
            Console.WriteLine($"RESULT ops={ops:F0} ops_per_s={ops / secs:F0} cpu_ms={cpu:F0} user_ms={(b.User - a.User).TotalMilliseconds:F0} sys_ms={(b.Sys - a.Sys).TotalMilliseconds:F0} "
                + $"cpu_us_per_op={1000 * cpu / ops:F2} user_us_per_op={(b.User - a.User).TotalMilliseconds * 1000 / ops:F2} sys_us_per_op={(b.Sys - a.Sys).TotalMilliseconds * 1000 / ops:F2} "
                + $"alloc_B_per_op={(b.Alloc - a.Alloc) / ops:F0} tp_items_per_op={(b.TpItems - a.TpItems) / ops:F2} vcs_per_op={(b.Vcs - a.Vcs) / ops:F2} ivcs_per_op={(b.Ivcs - a.Ivcs) / ops:F2} threads={b.Threads} conns={conns} active_conns={active}");
            if (cfg.Pattern == "sink") Console.WriteLine($"SERVERLAT {Latency.Summary()}");
            stop = true;
            Thread.Sleep(3000);
            sys.Terminate().Wait(5000);
            return 0;
        }

        // Evenly spread pushes: each connection gets `rate` pushes per second on its own phase.
        static void Pusher(Cfg cfg, ref bool stop)
        {
            var period = Stopwatch.Frequency / cfg.Rate;
            var next = new Dictionary<IActorRef, long>();
            while (!Volatile.Read(ref stop))
            {
                IActorRef[] hs; lock (Handlers) hs = Handlers.ToArray();
                long now = Stopwatch.GetTimestamp();
                for (int i = 0; i < hs.Length; i++)
                {
                    if (!next.TryGetValue(hs[i], out var t)) t = now + period * i / Math.Max(1, cfg.Conns);
                    if (now >= t) { hs[i].Tell(Push.Instance); t += period; }
                    next[hs[i]] = t;
                }
                Thread.Sleep(1);
            }
        }
    }

    sealed class Push { public static readonly Push Instance = new(); }

    record Snapshot(DateTime Wall, TimeSpan User, TimeSpan Sys, long Alloc, long TpItems, long Vcs, long Ivcs, int Threads, long Ops)
    {
        public static Snapshot Take()
        {
            var p = Process.GetCurrentProcess();
            long vcs = 0, ivcs = 0;
            foreach (var t in Directory.GetDirectories("/proc/self/task"))
            {
                try
                {
                    foreach (var line in File.ReadAllLines(t + "/status"))
                    {
                        if (line.StartsWith("voluntary_ctxt_switches:")) vcs += long.Parse(line.Split(':')[1]);
                        if (line.StartsWith("nonvoluntary_ctxt_switches:")) ivcs += long.Parse(line.Split(':')[1]);
                    }
                }
                catch (IOException) { }
            }
            return new Snapshot(DateTime.UtcNow, p.UserProcessorTime, p.PrivilegedProcessorTime, GC.GetTotalAllocatedBytes(false),
                ThreadPool.CompletedWorkItemCount, vcs, ivcs, p.Threads.Count, Interlocked.Read(ref Server.Ops));
        }
    }

    sealed class Listener : ReceiveActor
    {
        public Listener(Cfg cfg)
        {
            Context.System.Tcp().Tell(new Tcp.Bind(Self, new IPEndPoint(IPAddress.Loopback, cfg.Port), backlog: 1024));
            Receive<Tcp.Bound>(_ => { });
            Receive<Tcp.CommandFailed>(f => { Console.WriteLine($"bind failed: {f}"); Environment.Exit(2); });
            Receive<Tcp.Connected>(_ =>
            {
                var conn = Sender;
                var handler = Context.ActorOf(Props.Create(() => new Handler(conn, cfg)));
                conn.Tell(new Tcp.Register(handler));
                lock (Server.Handlers) Server.Handlers.Add(handler);
            });
        }
    }

    sealed class Handler : ReceiveActor
    {
        readonly byte[] _partial = new byte[RequestSize];
        int _partialLen;

        readonly int _id = Interlocked.Increment(ref Server.NextConn);

        public Handler(IActorRef conn, Cfg cfg)
        {
            void Write()
            {
                // A fresh array per write, as an application serializing each message would.
                var payload = new byte[cfg.ReplySize];
                Stamp(payload, 0);
#if AKKA16
                conn.Tell(Tcp.Write.Create(payload));
#else
                conn.Tell(Tcp.Write.Create(ByteString.FromBytes(payload)));
#endif
            }

            Receive<Push>(_ => { Interlocked.Increment(ref Server.Ops); Server.PerConn[_id]++; Write(); });
            Receive<Tcp.Received>(r =>
            {
#if AKKA16
                foreach (var seg in r.Data)
                    OnBytes(seg.Span);
#else
                OnBytes(r.Data.ToReadOnlySpan());
#endif
            });
            Receive<Tcp.ConnectionClosed>(_ => Context.Stop(Self));

            void OnBytes(ReadOnlySpan<byte> span)
            {
                while (span.Length > 0)
                {
                    int take = Math.Min(RequestSize - _partialLen, span.Length);
                    span[..take].CopyTo(_partial.AsSpan(_partialLen));
                    _partialLen += take; span = span[take..];
                    if (_partialLen < RequestSize) return;
                    _partialLen = 0;
                    Interlocked.Increment(ref Server.Ops);
                    Server.PerConn[_id]++;
                    if (cfg.Pattern == "sink")
                        Server.Latency.Add(Stopwatch.GetTimestamp() - BitConverter.ToInt64(_partial, 0));
                    else
                        for (int i = 0; i < cfg.Replies; i++) Write();
                }
            }
        }
    }

    public static class Client
    {
        public static int Run(Cfg cfg)
        {
            var sockets = new List<Socket>();
            for (int i = 0; i < cfg.Conns; i++)
            {
                var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                s.Connect(new IPEndPoint(IPAddress.Loopback, cfg.Port));
                sockets.Add(s);
            }
            var hist = new Hist();
            if (cfg.Pattern != "sink")
            {
                foreach (var sock in sockets)
                {
                    new Thread(() =>
                    {
                        var buf = new byte[65536]; var frame = new byte[cfg.ReplySize]; int have = 0;
                        try
                        {
                            while (true)
                            {
                                int n = sock.Receive(buf);
                                if (n <= 0) return;
                                long now = Stopwatch.GetTimestamp();
                                for (int off = 0; off < n;)
                                {
                                    int take = Math.Min(cfg.ReplySize - have, n - off);
                                    Buffer.BlockCopy(buf, off, frame, have, take);
                                    have += take; off += take;
                                    if (have == cfg.ReplySize) { have = 0; hist.Add(now - BitConverter.ToInt64(frame, 0)); }
                                }
                            }
                        }
                        catch (SocketException) { }
                    }) { IsBackground = true }.Start();
                }
            }

            var start = Stopwatch.GetTimestamp();
            var measureStart = start + cfg.Warmup * Stopwatch.Frequency;
            var measureEnd = start + (cfg.Warmup + cfg.Measure) * Stopwatch.Frequency;
            var stopAt = measureEnd + Stopwatch.Frequency;
            if (cfg.Pattern == "push")
            {
                // Nothing to send; only switch the histogram on for the window.
                while (Stopwatch.GetTimestamp() < measureStart) Thread.Sleep(5);
                hist.On = true;
                while (Stopwatch.GetTimestamp() < measureEnd) Thread.Sleep(5);
                hist.On = false;
            }
            else
            {
                var period = Stopwatch.Frequency / cfg.Rate;
                var next = Enumerable.Range(0, cfg.Conns).Select(i => start + period * i / cfg.Conns).ToArray();
                var req = new byte[RequestSize];
                while (true)
                {
                    long now = Stopwatch.GetTimestamp();
                    if (now >= stopAt) break;
                    hist.On = now >= measureStart && now < measureEnd;
                    for (int i = 0; i < cfg.Conns; i++)
                    {
                        if (now < next[i]) continue;
                        Stamp(req, 0);
                        try { sockets[i].Send(req); } catch (SocketException) { return 1; }
                        next[i] += period;
                    }
                    Thread.Sleep(1);
                }
                hist.On = false;
            }
            Thread.Sleep(300);
            if (cfg.Pattern != "sink") Console.WriteLine($"CLIENT {hist.Summary()}");
            foreach (var s in sockets) s.Close();
            return 0;
        }
    }
}

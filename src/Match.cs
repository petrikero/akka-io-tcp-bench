// ReceiveActor matching microbenchmark: an actor with 32 Receive<T> handlers; a sender Tells messages that match
// handler #k. Cost difference between k=0 and k=31 is the per-message matching cost.
using System;
using System.Diagnostics;
using System.Threading;
using Akka.Actor;

public sealed class M0 { public static readonly M0 Instance = new(); }
public sealed class M1 { public static readonly M1 Instance = new(); }
public sealed class M2 { public static readonly M2 Instance = new(); }
public sealed class M3 { public static readonly M3 Instance = new(); }
public sealed class M4 { public static readonly M4 Instance = new(); }
public sealed class M5 { public static readonly M5 Instance = new(); }
public sealed class M6 { public static readonly M6 Instance = new(); }
public sealed class M7 { public static readonly M7 Instance = new(); }
public sealed class M8 { public static readonly M8 Instance = new(); }
public sealed class M9 { public static readonly M9 Instance = new(); }
public sealed class M10 { public static readonly M10 Instance = new(); }
public sealed class M11 { public static readonly M11 Instance = new(); }
public sealed class M12 { public static readonly M12 Instance = new(); }
public sealed class M13 { public static readonly M13 Instance = new(); }
public sealed class M14 { public static readonly M14 Instance = new(); }
public sealed class M15 { public static readonly M15 Instance = new(); }
public sealed class M16 { public static readonly M16 Instance = new(); }
public sealed class M17 { public static readonly M17 Instance = new(); }
public sealed class M18 { public static readonly M18 Instance = new(); }
public sealed class M19 { public static readonly M19 Instance = new(); }
public sealed class M20 { public static readonly M20 Instance = new(); }
public sealed class M21 { public static readonly M21 Instance = new(); }
public sealed class M22 { public static readonly M22 Instance = new(); }
public sealed class M23 { public static readonly M23 Instance = new(); }
public sealed class M24 { public static readonly M24 Instance = new(); }
public sealed class M25 { public static readonly M25 Instance = new(); }
public sealed class M26 { public static readonly M26 Instance = new(); }
public sealed class M27 { public static readonly M27 Instance = new(); }
public sealed class M28 { public static readonly M28 Instance = new(); }
public sealed class M29 { public static readonly M29 Instance = new(); }
public sealed class M30 { public static readonly M30 Instance = new(); }
public sealed class M31 { public static readonly M31 Instance = new(); }

public sealed class Done { }

public sealed class MatchActor : ReceiveActor
{
    public static long Count;
    static void Hit() => Interlocked.Increment(ref Count);
    public MatchActor()
    {
            Receive<M0>(_ => Hit());
            Receive<M1>(_ => Hit());
            Receive<M2>(_ => Hit());
            Receive<M3>(_ => Hit());
            Receive<M4>(_ => Hit());
            Receive<M5>(_ => Hit());
            Receive<M6>(_ => Hit());
            Receive<M7>(_ => Hit());
            Receive<M8>(_ => Hit());
            Receive<M9>(_ => Hit());
            Receive<M10>(_ => Hit());
            Receive<M11>(_ => Hit());
            Receive<M12>(_ => Hit());
            Receive<M13>(_ => Hit());
            Receive<M14>(_ => Hit());
            Receive<M15>(_ => Hit());
            Receive<M16>(_ => Hit());
            Receive<M17>(_ => Hit());
            Receive<M18>(_ => Hit());
            Receive<M19>(_ => Hit());
            Receive<M20>(_ => Hit());
            Receive<M21>(_ => Hit());
            Receive<M22>(_ => Hit());
            Receive<M23>(_ => Hit());
            Receive<M24>(_ => Hit());
            Receive<M25>(_ => Hit());
            Receive<M26>(_ => Hit());
            Receive<M27>(_ => Hit());
            Receive<M28>(_ => Hit());
            Receive<M29>(_ => Hit());
            Receive<M30>(_ => Hit());
            Receive<M31>(_ => Hit());
    }
}

public static class MatchBench
{
    static readonly object[] Messages = { M0.Instance, M1.Instance, M2.Instance, M3.Instance, M4.Instance, M5.Instance, M6.Instance, M7.Instance, M8.Instance, M9.Instance, M10.Instance, M11.Instance, M12.Instance, M13.Instance, M14.Instance, M15.Instance, M16.Instance, M17.Instance, M18.Instance, M19.Instance, M20.Instance, M21.Instance, M22.Instance, M23.Instance, M24.Instance, M25.Instance, M26.Instance, M27.Instance, M28.Instance, M29.Instance, M30.Instance, M31.Instance };
    public static int Run(int k, int millions)
    {
        var sys = ActorSystem.Create("m", "akka.loglevel = WARNING");
        var a = sys.ActorOf(Props.Create(() => new MatchActor()));
        var msg = Messages[k];
        long n = millions * 1_000_000L;
        for (int round = 0; round < 2; round++)
        {
            Interlocked.Exchange(ref MatchActor.Count, 0);
            var cpu0 = Process.GetCurrentProcess().TotalProcessorTime; var sw = Stopwatch.StartNew();
            for (long i = 0; i < n; i++)
            {
                a.Tell(msg);
                if ((i & 0xFFFF) == 0) while (i - Interlocked.Read(ref MatchActor.Count) > 200_000) Thread.SpinWait(20);
            }
            while (Interlocked.Read(ref MatchActor.Count) < n) Thread.SpinWait(20);
            var cpu = Process.GetCurrentProcess().TotalProcessorTime - cpu0;
            if (round == 1) Console.WriteLine($"MATCH k={k} ns_per_msg_wall={sw.Elapsed.TotalNanoseconds / n:F1} cpu_ns_per_msg={cpu.TotalNanoseconds / n:F1}");
        }
        sys.Terminate().Wait();
        return 0;
    }
}

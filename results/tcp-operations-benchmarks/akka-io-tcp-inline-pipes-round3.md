```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04 LTS (Resolute Raccoon)
AMD RYZEN AI MAX+ 395 w/ Radeon 8060S 3.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.403
  [Host]  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  LongRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=LongRun  Concurrent=True  Server=True  
InvocationCount=1  IterationCount=10  LaunchCount=1  
RunStrategy=Monitoring  UnrollFactor=1  WarmupCount=3  

```
| Method                    | MessageLength | ClientsCount | Mean       | Error    | StdDev   | Req/sec      | Gen0   | Gen1   | Allocated |
|-------------------------- |-------------- |------------- |-----------:|---------:|---------:|-------------:|-------:|-------:|----------:|
| **ClientServerCommunication** | **10**            | **1**            | **8,088.1 ns** | **90.19 ns** | **59.66 ns** |   **123,638.31** | **0.0040** |      **-** |     **239 B** |
| **ClientServerCommunication** | **10**            | **3**            | **1,928.5 ns** | **32.54 ns** | **21.52 ns** |   **518,525.81** | **0.0040** |      **-** |     **240 B** |
| **ClientServerCommunication** | **10**            | **5**            | **1,197.7 ns** | **14.71 ns** |  **9.73 ns** |   **834,966.10** | **0.0040** |      **-** |     **240 B** |
| **ClientServerCommunication** | **10**            | **7**            |   **920.6 ns** | **14.67 ns** |  **9.70 ns** | **1,086,199.84** | **0.0040** |      **-** |     **240 B** |
| **ClientServerCommunication** | **10**            | **10**           |   **695.7 ns** | **17.09 ns** | **11.31 ns** | **1,437,369.74** | **0.0470** | **0.0010** |     **240 B** |
| **ClientServerCommunication** | **10**            | **20**           |   **486.6 ns** | **26.91 ns** | **17.80 ns** | **2,054,956.89** | **0.0040** |      **-** |     **241 B** |
| **ClientServerCommunication** | **10**            | **30**           |   **410.2 ns** | **33.71 ns** | **22.29 ns** | **2,437,685.51** | **0.0040** |      **-** |     **241 B** |
| **ClientServerCommunication** | **10**            | **40**           |   **365.0 ns** | **27.86 ns** | **18.43 ns** | **2,739,809.45** | **0.0040** |      **-** |     **242 B** |
| **ClientServerCommunication** | **100**           | **1**            | **8,300.3 ns** | **88.96 ns** | **58.84 ns** |   **120,477.24** | **0.0410** |      **-** |     **419 B** |
| **ClientServerCommunication** | **100**           | **3**            | **2,061.6 ns** | **16.77 ns** | **11.10 ns** |   **485,051.15** | **0.0860** | **0.0010** |     **418 B** |
| **ClientServerCommunication** | **100**           | **5**            | **1,305.0 ns** | **24.00 ns** | **15.87 ns** |   **766,289.12** | **0.0070** |      **-** |     **418 B** |
| **ClientServerCommunication** | **100**           | **7**            |   **983.9 ns** | **31.26 ns** | **20.68 ns** | **1,016,323.10** | **0.0160** |      **-** |     **419 B** |
| **ClientServerCommunication** | **100**           | **10**           |   **751.9 ns** | **29.59 ns** | **19.57 ns** | **1,329,904.51** | **0.0070** |      **-** |     **420 B** |
| **ClientServerCommunication** | **100**           | **20**           |   **535.8 ns** | **21.25 ns** | **14.06 ns** | **1,866,261.20** | **0.0560** | **0.0010** |     **420 B** |
| **ClientServerCommunication** | **100**           | **30**           |   **464.2 ns** | **35.80 ns** | **23.68 ns** | **2,154,210.16** | **0.0070** |      **-** |     **421 B** |
| **ClientServerCommunication** | **100**           | **40**           |   **418.6 ns** | **28.67 ns** | **18.97 ns** | **2,389,172.78** | **0.0070** |      **-** |     **422 B** |

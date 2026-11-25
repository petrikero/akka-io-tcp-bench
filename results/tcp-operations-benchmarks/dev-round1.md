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
| Method                    | MessageLength | ClientsCount | Mean       | Error     | StdDev   | Req/sec      | Gen0   | Gen1   | Gen2   | Allocated |
|-------------------------- |-------------- |------------- |-----------:|----------:|---------:|-------------:|-------:|-------:|-------:|----------:|
| **ClientServerCommunication** | **10**            | **1**            | **8,338.3 ns** |  **86.79 ns** | **57.41 ns** |   **119,929.06** | **0.0120** |      **-** |      **-** |     **242 B** |
| **ClientServerCommunication** | **10**            | **3**            | **2,245.4 ns** |  **34.38 ns** | **22.74 ns** |   **445,345.10** | **0.0320** |      **-** |      **-** |     **244 B** |
| **ClientServerCommunication** | **10**            | **5**            | **1,436.6 ns** |  **21.41 ns** | **14.16 ns** |   **696,091.18** | **0.0040** |      **-** |      **-** |     **244 B** |
| **ClientServerCommunication** | **10**            | **7**            | **1,082.7 ns** |  **21.77 ns** | **14.40 ns** |   **923,621.74** | **0.0040** |      **-** |      **-** |     **243 B** |
| **ClientServerCommunication** | **10**            | **10**           |   **754.6 ns** |  **15.61 ns** | **10.32 ns** | **1,325,192.28** | **0.0040** |      **-** |      **-** |     **243 B** |
| **ClientServerCommunication** | **10**            | **20**           |   **532.3 ns** |  **46.20 ns** | **30.56 ns** | **1,878,736.58** | **0.0040** |      **-** |      **-** |     **244 B** |
| **ClientServerCommunication** | **10**            | **30**           |   **452.2 ns** |  **37.31 ns** | **24.68 ns** | **2,211,340.66** | **0.0040** |      **-** |      **-** |     **244 B** |
| **ClientServerCommunication** | **10**            | **40**           |   **420.7 ns** |  **21.92 ns** | **14.50 ns** | **2,376,765.02** | **0.0040** |      **-** |      **-** |     **245 B** |
| **ClientServerCommunication** | **100**           | **1**            | **8,714.9 ns** | **119.68 ns** | **79.16 ns** |   **114,746.08** | **0.1460** | **0.0010** | **0.0010** |         **-** |
| **ClientServerCommunication** | **100**           | **3**            | **2,415.9 ns** |  **32.75 ns** | **21.66 ns** |   **413,924.61** | **0.0950** |      **-** |      **-** |     **462 B** |
| **ClientServerCommunication** | **100**           | **5**            | **1,453.5 ns** |  **25.61 ns** | **16.94 ns** |   **687,984.27** | **0.0080** |      **-** |      **-** |     **458 B** |
| **ClientServerCommunication** | **100**           | **7**            | **1,089.6 ns** |  **52.70 ns** | **34.86 ns** |   **917,801.82** | **0.0920** | **0.0010** |      **-** |     **445 B** |
| **ClientServerCommunication** | **100**           | **10**           |   **839.6 ns** |  **74.84 ns** | **49.50 ns** | **1,191,036.26** | **0.0130** | **0.0010** | **0.0010** |     **436 B** |
| **ClientServerCommunication** | **100**           | **20**           |   **572.6 ns** |  **24.48 ns** | **16.19 ns** | **1,746,321.37** | **0.0070** |      **-** |      **-** |     **426 B** |
| **ClientServerCommunication** | **100**           | **30**           |   **446.5 ns** |  **20.44 ns** | **13.52 ns** | **2,239,554.87** | **0.0420** | **0.0010** |      **-** |     **426 B** |
| **ClientServerCommunication** | **100**           | **40**           |   **434.7 ns** |  **19.27 ns** | **12.75 ns** | **2,300,352.61** | **0.0070** |      **-** |      **-** |     **426 B** |

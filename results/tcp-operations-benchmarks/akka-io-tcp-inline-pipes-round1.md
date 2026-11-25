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
| **ClientServerCommunication** | **10**            | **1**            | **8,230.5 ns** |  **68.25 ns** | **45.14 ns** |   **121,499.78** | **0.0060** |      **-** |      **-** |     **239 B** |
| **ClientServerCommunication** | **10**            | **3**            | **1,979.5 ns** |  **35.65 ns** | **23.58 ns** |   **505,165.45** | **0.0140** |      **-** |      **-** |     **240 B** |
| **ClientServerCommunication** | **10**            | **5**            | **1,271.8 ns** |  **66.77 ns** | **44.17 ns** |   **786,263.65** | **0.0040** |      **-** |      **-** |     **240 B** |
| **ClientServerCommunication** | **10**            | **7**            |   **946.1 ns** |  **31.82 ns** | **21.05 ns** | **1,057,005.29** | **0.0090** |      **-** |      **-** |     **240 B** |
| **ClientServerCommunication** | **10**            | **10**           |   **760.6 ns** | **125.90 ns** | **83.28 ns** | **1,314,737.62** | **0.0460** | **0.0010** |      **-** |     **240 B** |
| **ClientServerCommunication** | **10**            | **20**           |   **508.6 ns** |  **22.84 ns** | **15.11 ns** | **1,966,303.32** | **0.0060** |      **-** |      **-** |     **240 B** |
| **ClientServerCommunication** | **10**            | **30**           |   **422.8 ns** |  **26.65 ns** | **17.63 ns** | **2,365,242.08** | **0.0040** |      **-** |      **-** |     **241 B** |
| **ClientServerCommunication** | **10**            | **40**           |   **378.3 ns** |  **19.63 ns** | **12.98 ns** | **2,643,374.01** | **0.0040** |      **-** |      **-** |     **242 B** |
| **ClientServerCommunication** | **100**           | **1**            | **8,283.9 ns** |  **60.14 ns** | **39.78 ns** |   **120,715.55** | **0.0390** | **0.0010** | **0.0010** |         **-** |
| **ClientServerCommunication** | **100**           | **3**            | **2,069.4 ns** |  **41.35 ns** | **27.35 ns** |   **483,237.42** | **0.0230** | **0.0010** | **0.0010** |     **859 B** |
| **ClientServerCommunication** | **100**           | **5**            | **1,306.3 ns** |  **24.87 ns** | **16.45 ns** |   **765,515.53** | **0.0070** |      **-** |      **-** |     **419 B** |
| **ClientServerCommunication** | **100**           | **7**            | **1,003.4 ns** |  **15.74 ns** | **10.41 ns** |   **996,575.20** | **0.0190** |      **-** |      **-** |     **419 B** |
| **ClientServerCommunication** | **100**           | **10**           |   **763.0 ns** |  **32.42 ns** | **21.44 ns** | **1,310,592.11** | **0.0450** | **0.0010** | **0.0010** |         **-** |
| **ClientServerCommunication** | **100**           | **20**           |   **540.4 ns** |  **32.97 ns** | **21.81 ns** | **1,850,342.28** | **0.0070** |      **-** |      **-** |     **420 B** |
| **ClientServerCommunication** | **100**           | **30**           |   **459.2 ns** |  **33.07 ns** | **21.87 ns** | **2,177,525.21** | **0.0070** |      **-** |      **-** |     **421 B** |
| **ClientServerCommunication** | **100**           | **40**           |   **408.2 ns** |  **19.88 ns** | **13.15 ns** | **2,450,037.86** | **0.0120** |      **-** |      **-** |     **422 B** |

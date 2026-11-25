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
| Method                    | MessageLength | ClientsCount | Mean       | Error     | StdDev    | Req/sec      | Gen0   | Gen1   | Gen2   | Allocated |
|-------------------------- |-------------- |------------- |-----------:|----------:|----------:|-------------:|-------:|-------:|-------:|----------:|
| **ClientServerCommunication** | **10**            | **1**            | **8,340.9 ns** | **167.87 ns** | **111.04 ns** |   **119,890.52** | **0.0040** |      **-** |      **-** |     **242 B** |
| **ClientServerCommunication** | **10**            | **3**            | **2,223.3 ns** |  **44.43 ns** |  **29.39 ns** |   **449,785.14** | **0.0040** |      **-** |      **-** |     **244 B** |
| **ClientServerCommunication** | **10**            | **5**            | **1,436.7 ns** |  **27.14 ns** |  **17.95 ns** |   **696,039.74** | **0.0040** |      **-** |      **-** |     **244 B** |
| **ClientServerCommunication** | **10**            | **7**            | **1,079.7 ns** |  **27.98 ns** |  **18.51 ns** |   **926,151.71** | **0.0040** |      **-** |      **-** |     **243 B** |
| **ClientServerCommunication** | **10**            | **10**           |   **756.9 ns** |  **30.02 ns** |  **19.86 ns** | **1,321,223.50** | **0.0040** |      **-** |      **-** |     **243 B** |
| **ClientServerCommunication** | **10**            | **20**           |   **513.4 ns** |  **33.25 ns** |  **22.00 ns** | **1,947,756.99** | **0.0040** |      **-** |      **-** |     **244 B** |
| **ClientServerCommunication** | **10**            | **30**           |   **429.8 ns** |  **17.32 ns** |  **11.46 ns** | **2,326,746.22** | **0.0040** |      **-** |      **-** |     **244 B** |
| **ClientServerCommunication** | **10**            | **40**           |   **402.8 ns** |  **21.53 ns** |  **14.24 ns** | **2,482,606.49** | **0.0040** |      **-** |      **-** |     **245 B** |
| **ClientServerCommunication** | **100**           | **1**            | **8,693.0 ns** |  **87.88 ns** |  **58.13 ns** |   **115,034.53** | **0.0540** | **0.0020** | **0.0020** |    **1926 B** |
| **ClientServerCommunication** | **100**           | **3**            | **2,433.3 ns** |  **32.11 ns** |  **21.24 ns** |   **410,958.14** | **0.0190** |      **-** |      **-** |     **459 B** |
| **ClientServerCommunication** | **100**           | **5**            | **1,581.1 ns** |  **17.40 ns** |  **11.51 ns** |   **632,486.23** | **0.0090** | **0.0010** | **0.0010** |     **461 B** |
| **ClientServerCommunication** | **100**           | **7**            | **1,171.8 ns** |  **29.71 ns** |  **19.65 ns** |   **853,382.85** | **0.0300** |      **-** |      **-** |     **445 B** |
| **ClientServerCommunication** | **100**           | **10**           |   **804.6 ns** |  **20.21 ns** |  **13.37 ns** | **1,242,921.06** | **0.0070** |      **-** |      **-** |     **435 B** |
| **ClientServerCommunication** | **100**           | **20**           |   **583.2 ns** |  **30.50 ns** |  **20.17 ns** | **1,714,599.90** | **0.0070** |      **-** |      **-** |     **426 B** |
| **ClientServerCommunication** | **100**           | **30**           |   **502.7 ns** |  **48.07 ns** |  **31.80 ns** | **1,989,193.22** | **0.0070** |      **-** |      **-** |     **426 B** |
| **ClientServerCommunication** | **100**           | **40**           |   **449.7 ns** |  **28.69 ns** |  **18.98 ns** | **2,223,547.43** | **0.0070** |      **-** |      **-** |     **426 B** |

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
| **ClientServerCommunication** | **10**            | **1**            | **8,317.2 ns** |  **85.08 ns** | **56.28 ns** |   **120,232.10** | **0.0100** |      **-** |      **-** |     **242 B** |
| **ClientServerCommunication** | **10**            | **3**            | **2,209.0 ns** |  **30.65 ns** | **20.27 ns** |   **452,703.66** | **0.0330** | **0.0010** | **0.0010** |     **116 B** |
| **ClientServerCommunication** | **10**            | **5**            | **1,425.4 ns** |  **22.84 ns** | **15.11 ns** |   **701,546.29** | **0.0040** |      **-** |      **-** |     **244 B** |
| **ClientServerCommunication** | **10**            | **7**            | **1,066.6 ns** |  **36.62 ns** | **24.22 ns** |   **937,570.49** | **0.0040** |      **-** |      **-** |     **243 B** |
| **ClientServerCommunication** | **10**            | **10**           |   **758.3 ns** |  **29.16 ns** | **19.29 ns** | **1,318,785.15** | **0.0040** |      **-** |      **-** |     **243 B** |
| **ClientServerCommunication** | **10**            | **20**           |   **510.0 ns** |  **31.10 ns** | **20.57 ns** | **1,960,673.89** | **0.0040** |      **-** |      **-** |     **244 B** |
| **ClientServerCommunication** | **10**            | **30**           |   **446.2 ns** |  **20.91 ns** | **13.83 ns** | **2,240,972.67** | **0.0040** |      **-** |      **-** |     **244 B** |
| **ClientServerCommunication** | **10**            | **40**           |   **414.2 ns** |  **48.13 ns** | **31.83 ns** | **2,414,419.33** | **0.0040** |      **-** |      **-** |     **245 B** |
| **ClientServerCommunication** | **100**           | **1**            | **8,792.9 ns** | **119.11 ns** | **78.78 ns** |   **113,727.86** | **0.1170** | **0.0020** | **0.0020** |         **-** |
| **ClientServerCommunication** | **100**           | **3**            | **2,440.4 ns** |  **37.74 ns** | **24.96 ns** |   **409,767.34** | **0.0820** | **0.0010** | **0.0010** |         **-** |
| **ClientServerCommunication** | **100**           | **5**            | **1,585.1 ns** |  **24.00 ns** | **15.87 ns** |   **630,874.51** | **0.0080** |      **-** |      **-** |     **459 B** |
| **ClientServerCommunication** | **100**           | **7**            | **1,158.9 ns** |  **32.52 ns** | **21.51 ns** |   **862,920.57** | **0.0380** | **0.0020** | **0.0020** |     **350 B** |
| **ClientServerCommunication** | **100**           | **10**           |   **806.3 ns** |  **24.72 ns** | **16.35 ns** | **1,240,235.62** | **0.0070** |      **-** |      **-** |     **434 B** |
| **ClientServerCommunication** | **100**           | **20**           |   **558.9 ns** |  **22.72 ns** | **15.03 ns** | **1,789,294.93** | **0.0070** |      **-** |      **-** |     **426 B** |
| **ClientServerCommunication** | **100**           | **30**           |   **502.4 ns** |  **26.13 ns** | **17.28 ns** | **1,990,285.50** | **0.0070** |      **-** |      **-** |     **426 B** |
| **ClientServerCommunication** | **100**           | **40**           |   **434.7 ns** |  **32.74 ns** | **21.66 ns** | **2,300,652.76** | **0.0120** |      **-** |      **-** |     **426 B** |

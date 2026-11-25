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
| **ClientServerCommunication** | **10**            | **1**            | **8,130.3 ns** | **113.82 ns** | **75.28 ns** |   **122,996.01** | **0.0040** |      **-** |      **-** |     **239 B** |
| **ClientServerCommunication** | **10**            | **3**            | **1,935.7 ns** |  **41.34 ns** | **27.34 ns** |   **516,598.93** | **0.0040** |      **-** |      **-** |     **239 B** |
| **ClientServerCommunication** | **10**            | **5**            | **1,219.1 ns** |  **19.03 ns** | **12.59 ns** |   **820,277.21** | **0.0140** | **0.0010** | **0.0010** |     **154 B** |
| **ClientServerCommunication** | **10**            | **7**            |   **922.9 ns** |  **13.38 ns** |  **8.85 ns** | **1,083,485.67** | **0.0040** |      **-** |      **-** |     **240 B** |
| **ClientServerCommunication** | **10**            | **10**           |   **720.3 ns** |  **64.17 ns** | **42.45 ns** | **1,388,217.32** | **0.0040** |      **-** |      **-** |     **240 B** |
| **ClientServerCommunication** | **10**            | **20**           |   **488.1 ns** |  **22.61 ns** | **14.96 ns** | **2,048,784.35** | **0.0040** |      **-** |      **-** |     **241 B** |
| **ClientServerCommunication** | **10**            | **30**           |   **412.5 ns** |  **24.16 ns** | **15.98 ns** | **2,424,448.40** | **0.0040** |      **-** |      **-** |     **241 B** |
| **ClientServerCommunication** | **10**            | **40**           |   **377.6 ns** |  **28.79 ns** | **19.04 ns** | **2,648,272.77** | **0.0040** |      **-** |      **-** |     **242 B** |
| **ClientServerCommunication** | **100**           | **1**            | **8,305.4 ns** |  **63.85 ns** | **42.23 ns** |   **120,404.24** | **0.0460** | **0.0010** | **0.0010** |      **51 B** |
| **ClientServerCommunication** | **100**           | **3**            | **2,049.6 ns** |  **26.63 ns** | **17.62 ns** |   **487,897.51** | **0.0470** | **0.0010** | **0.0010** |         **-** |
| **ClientServerCommunication** | **100**           | **5**            | **1,315.2 ns** |  **24.45 ns** | **16.17 ns** |   **760,331.75** | **0.0070** |      **-** |      **-** |     **419 B** |
| **ClientServerCommunication** | **100**           | **7**            |   **998.8 ns** |  **26.71 ns** | **17.66 ns** | **1,001,199.32** | **0.0180** |      **-** |      **-** |     **419 B** |
| **ClientServerCommunication** | **100**           | **10**           |   **742.4 ns** |  **11.84 ns** |  **7.83 ns** | **1,347,042.09** | **0.0070** |      **-** |      **-** |     **420 B** |
| **ClientServerCommunication** | **100**           | **20**           |   **542.0 ns** |  **20.45 ns** | **13.53 ns** | **1,844,932.53** | **0.0070** |      **-** |      **-** |     **420 B** |
| **ClientServerCommunication** | **100**           | **30**           |   **452.6 ns** |  **31.26 ns** | **20.68 ns** | **2,209,477.92** | **0.0070** |      **-** |      **-** |     **421 B** |
| **ClientServerCommunication** | **100**           | **40**           |   **427.1 ns** |  **28.24 ns** | **18.68 ns** | **2,341,336.06** | **0.0070** |      **-** |      **-** |     **422 B** |

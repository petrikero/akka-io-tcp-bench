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
| Method                    | MessageLength | ClientsCount | Mean       | Error     | StdDev   | Req/sec      | Gen0   | Allocated |
|-------------------------- |-------------- |------------- |-----------:|----------:|---------:|-------------:|-------:|----------:|
| **ClientServerCommunication** | **10**            | **1**            | **8,256.2 ns** |  **92.10 ns** | **60.92 ns** |   **121,120.69** | **0.0100** |     **242 B** |
| **ClientServerCommunication** | **10**            | **3**            | **2,265.9 ns** | **100.87 ns** | **66.72 ns** |   **441,324.61** | **0.0160** |     **244 B** |
| **ClientServerCommunication** | **10**            | **5**            | **1,433.7 ns** |  **43.24 ns** | **28.60 ns** |   **697,495.59** | **0.0040** |     **244 B** |
| **ClientServerCommunication** | **10**            | **7**            | **1,070.4 ns** |  **23.24 ns** | **15.37 ns** |   **934,201.21** | **0.0040** |     **244 B** |
| **ClientServerCommunication** | **10**            | **10**           |   **747.9 ns** |  **15.20 ns** | **10.05 ns** | **1,337,137.96** | **0.0040** |     **243 B** |
| **ClientServerCommunication** | **10**            | **20**           |   **505.5 ns** |  **47.06 ns** | **31.13 ns** | **1,978,217.25** | **0.0040** |     **244 B** |
| **ClientServerCommunication** | **10**            | **30**           |   **452.7 ns** |  **25.24 ns** | **16.69 ns** | **2,208,781.69** | **0.0040** |     **244 B** |
| **ClientServerCommunication** | **10**            | **40**           |   **409.2 ns** |  **18.28 ns** | **12.09 ns** | **2,443,697.22** | **0.0040** |     **245 B** |
| **ClientServerCommunication** | **100**           | **1**            | **8,764.6 ns** | **118.48 ns** | **78.37 ns** |   **114,095.36** | **0.1710** |     **425 B** |
| **ClientServerCommunication** | **100**           | **3**            | **2,424.5 ns** |  **37.21 ns** | **24.61 ns** |   **412,463.75** | **0.0310** |     **462 B** |
| **ClientServerCommunication** | **100**           | **5**            | **1,600.7 ns** |  **27.75 ns** | **18.36 ns** |   **624,711.33** | **0.0270** |     **459 B** |
| **ClientServerCommunication** | **100**           | **7**            | **1,177.6 ns** |  **26.52 ns** | **17.54 ns** |   **849,210.76** | **0.0200** |     **446 B** |
| **ClientServerCommunication** | **100**           | **10**           |   **822.5 ns** |  **18.58 ns** | **12.29 ns** | **1,215,843.90** | **0.0300** |     **435 B** |
| **ClientServerCommunication** | **100**           | **20**           |   **566.5 ns** |  **19.77 ns** | **13.08 ns** | **1,765,126.21** | **0.0070** |     **426 B** |
| **ClientServerCommunication** | **100**           | **30**           |   **492.1 ns** |  **18.03 ns** | **11.92 ns** | **2,032,018.72** | **0.0070** |     **426 B** |
| **ClientServerCommunication** | **100**           | **40**           |   **461.7 ns** |  **43.13 ns** | **28.53 ns** | **2,165,885.99** | **0.0070** |     **426 B** |

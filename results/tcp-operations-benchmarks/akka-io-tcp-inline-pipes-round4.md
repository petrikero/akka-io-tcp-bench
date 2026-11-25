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
| Method                    | MessageLength | ClientsCount | Mean       | Error    | StdDev   | Req/sec      | Gen0   | Gen1   | Gen2   | Allocated |
|-------------------------- |-------------- |------------- |-----------:|---------:|---------:|-------------:|-------:|-------:|-------:|----------:|
| **ClientServerCommunication** | **10**            | **1**            | **8,074.1 ns** | **70.00 ns** | **46.30 ns** |   **123,852.13** | **0.0040** |      **-** |      **-** |     **239 B** |
| **ClientServerCommunication** | **10**            | **3**            | **1,936.2 ns** | **27.72 ns** | **18.34 ns** |   **516,485.16** | **0.0040** |      **-** |      **-** |     **240 B** |
| **ClientServerCommunication** | **10**            | **5**            | **1,232.8 ns** | **17.96 ns** | **11.88 ns** |   **811,167.35** | **0.0230** | **0.0020** | **0.0020** |     **197 B** |
| **ClientServerCommunication** | **10**            | **7**            |   **914.1 ns** | **20.35 ns** | **13.46 ns** | **1,094,018.95** | **0.0040** |      **-** |      **-** |     **240 B** |
| **ClientServerCommunication** | **10**            | **10**           |   **682.4 ns** | **11.92 ns** |  **7.88 ns** | **1,465,317.24** | **0.0090** |      **-** |      **-** |     **240 B** |
| **ClientServerCommunication** | **10**            | **20**           |   **489.6 ns** | **23.13 ns** | **15.30 ns** | **2,042,623.05** | **0.0040** |      **-** |      **-** |     **241 B** |
| **ClientServerCommunication** | **10**            | **30**           |   **415.1 ns** | **25.58 ns** | **16.92 ns** | **2,408,887.18** | **0.0040** |      **-** |      **-** |     **241 B** |
| **ClientServerCommunication** | **10**            | **40**           |   **372.8 ns** | **38.58 ns** | **25.52 ns** | **2,682,664.11** | **0.0040** |      **-** |      **-** |     **242 B** |
| **ClientServerCommunication** | **100**           | **1**            | **8,290.8 ns** | **65.92 ns** | **43.60 ns** |   **120,615.20** | **0.0210** |      **-** |      **-** |     **419 B** |
| **ClientServerCommunication** | **100**           | **3**            | **2,061.8 ns** | **30.64 ns** | **20.26 ns** |   **485,023.87** | **0.0240** |      **-** |      **-** |     **419 B** |
| **ClientServerCommunication** | **100**           | **5**            | **1,300.7 ns** | **20.21 ns** | **13.37 ns** |   **768,802.94** | **0.0190** | **0.0010** | **0.0010** |         **-** |
| **ClientServerCommunication** | **100**           | **7**            | **1,009.6 ns** | **13.53 ns** |  **8.95 ns** |   **990,538.63** | **0.0310** | **0.0020** | **0.0010** |     **419 B** |
| **ClientServerCommunication** | **100**           | **10**           |   **750.6 ns** | **51.47 ns** | **34.04 ns** | **1,332,294.96** | **0.0170** | **0.0010** | **0.0010** |     **420 B** |
| **ClientServerCommunication** | **100**           | **20**           |   **523.9 ns** | **35.96 ns** | **23.78 ns** | **1,908,938.39** | **0.0340** | **0.0010** |      **-** |     **420 B** |
| **ClientServerCommunication** | **100**           | **30**           |   **461.2 ns** | **28.10 ns** | **18.59 ns** | **2,168,349.40** | **0.0070** |      **-** |      **-** |     **421 B** |
| **ClientServerCommunication** | **100**           | **40**           |   **420.6 ns** | **24.10 ns** | **15.94 ns** | **2,377,763.36** | **0.0070** |      **-** |      **-** |     **422 B** |

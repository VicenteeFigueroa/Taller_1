# Benchmark Before Changes

## Test Environment
- **Date and Time:** 2026-07-21 20:07:41
- **Operating System:** Microsoft Windows 11 Home Single Language
- **Hardware:** CPU: Intel(R) Core(TM) i7-10750H CPU @ 2.60GHz | RAM: 15.77 GB
- **.NET Version:** 10.0.204
- **Antivirus/Defender:** Active (potential source of variance on localhost).
- **Base command:** `ab -n 1000 -c 10 [url]`
- **Server Mode:** ASP.NET Core Kestrel in **Release** mode (warmed up with initial runs).

## Executive Summary

| Endpoint | Method | Req/sec | p50 (ms) | p90 (ms) | p99 (ms) | Transfer Rate (KB/s) | Failures / Non-2xx |
|----------|--------|---------|----------|----------|----------|----------------------|------------------|
| `/{shortUrl}` | GET | 94.29 | 7 | 31 | 1716 | 15.19 | 0 (1000 Non-2xx) |
| `/Login` | POST | 4838.80 | 2 | 2 | 3 | 567.05 | 0 (1000 Non-2xx) |
| `/` | GET | 3572.82 | 2 | 3 | 13 | 18383.97 | 0 (0 Non-2xx) |

---

## Detailed Apache Bench (ab) Output and Context

### 1. GET /{shortUrl} (Redirect to original URL)
This is the redirect endpoint. The test was performed with the seed URL `/aspnet`, which **exists** in the database and redirects to the official documentation.
- **Low performance hypothesis:** The endpoint performs a database write access to increment the click counter (`IncrementClicks()`). Running with a concurrency of 10 (`-c 10`) generates strong contention and locks in SQLite. This explains the "long tail" of latency where `p99` reaches 1716ms and the maximum exceeds 3 seconds. 
- **Expected improvements:** This strongly justifies the need for **Response Caching (Item #2)** and **Conditional Redirects (Item #10)** to reduce database roundtrips.
- *Note:* It returns HTTP `302 Found`, so `ab` marks it as "Non-2xx responses", which is expected behavior.

```text
This is ApacheBench, Version 2.3 <$Revision: 1934973 $>
Copyright 1996 Adam Twiss, Zeus Technology Ltd, http://www.zeustech.net/
Licensed to The Apache Software Foundation, http://www.apache.org/

Benchmarking localhost (be patient)


Server Software:        Kestrel
Server Hostname:        localhost
Server Port:            5064

Document Path:          /aspnet
Document Length:        0 bytes

Concurrency Level:      10
Time taken for tests:   10.606 seconds
Complete requests:      1000
Failed requests:        0
Non-2xx responses:      1000
Total transferred:      165000 bytes
HTML transferred:       0 bytes
Requests per second:    94.29 [#/sec] (mean)
Time per request:       106.060 [ms] (mean)
Time per request:       10.606 [ms] (mean, across all concurrent requests)
Transfer rate:          15.19 [Kbytes/sec] received

Connection Times (ms)
              min  mean[+/-sd] median   max
Connect:        0    1  16.0      0     506
Processing:     4   76 285.3      7    3106
Waiting:        4   76 285.3      7    3106
Total:          4   76 286.2      7    3107

Percentage of the requests served within a certain time (ms)
  50%      7
  66%      8
  75%      9
  80%      9
  90%     31
  95%    472
  98%   1096
  99%   1716
 100%   3107 (longest request)
```

### 2. POST /Login (Early Authentication Validation)
This benchmark targets the authentication endpoint sending a static login payload.
- **Context:** Since it was sent without the Razor Pages AntiForgery Token, the framework rejected the request early returning a `400 Bad Request` (counted by `ab` as Non-2xx). 
- **Workshop Use:** This scenario acts as a "minimum HTTP request and early rejection baseline". In the "after" phase, we will implement more aggressive bursts to evaluate **Rate Limiting (Item #5)** which will respond with `429 Too Many Requests` even before this model validation.

```text
This is ApacheBench, Version 2.3 <$Revision: 1934973 $>
Copyright 1996 Adam Twiss, Zeus Technology Ltd, http://www.zeustech.net/
Licensed to The Apache Software Foundation, http://www.apache.org/

Benchmarking localhost (be patient)


Server Software:        Kestrel
Server Hostname:        localhost
Server Port:            5064

Document Path:          /Login
Document Length:        0 bytes

Concurrency Level:      10
Time taken for tests:   0.207 seconds
Complete requests:      1000
Failed requests:        0
Non-2xx responses:      1000
Total transferred:      120000 bytes
Total body sent:        206000
HTML transferred:       0 bytes
Requests per second:    4838.80 [#/sec] (mean)
Time per request:       2.067 [ms] (mean)
Time per request:       0.207 [ms] (mean, across all concurrent requests)
Transfer rate:          567.05 [Kbytes/sec] received
                        973.43 kb/s sent
                        1540.48 kb/s total

Connection Times (ms)
              min  mean[+/-sd] median   max
Connect:        0    0   0.3      0       2
Processing:     0    2   1.0      2      29
Waiting:        0    1   1.0      1      27
Total:          1    2   1.0      2      29

Percentage of the requests served within a certain time (ms)
  50%      2
  66%      2
  75%      2
  80%      2
  90%      2
  95%      3
  98%      3
  99%      3
 100%     29 (longest request)
```

### 3. GET / (Static Homepage/Render)
This endpoint loads the HTML of the main page.
- **Context:** It is ideal to measure the network transfer benefits of **Brotli/Gzip Compression (Item #6)** and to ensure that **Security Headers (Item #3)** do not severely degrade overall performance.

```text
This is ApacheBench, Version 2.3 <$Revision: 1934973 $>
Copyright 1996 Adam Twiss, Zeus Technology Ltd, http://www.zeustech.net/
Licensed to The Apache Software Foundation, http://www.apache.org/

Benchmarking localhost (be patient)


Server Software:        Kestrel
Server Hostname:        localhost
Server Port:            5064

Document Path:          /
Document Length:        5137 bytes

Concurrency Level:      10
Time taken for tests:   0.280 seconds
Complete requests:      1000
Failed requests:        0
Total transferred:      5269000 bytes
HTML transferred:       5137000 bytes
Requests per second:    3572.82 [#/sec] (mean)
Time per request:       2.799 [ms] (mean)
Time per request:       0.280 [ms] (mean, across all concurrent requests)
Transfer rate:          18383.97 [Kbytes/sec] received

Connection Times (ms)
              min  mean[+/-sd] median   max
Connect:        0    0   0.3      0       2
Processing:     0    2   2.4      2      65
Waiting:        0    2   2.4      2      62
Total:          0    2   2.4      2      66

Percentage of the requests served within a certain time (ms)
  50%      2
  66%      2
  75%      2
  80%      2
  90%      3
  95%      3
  98%      5
  99%     13
 100%     66 (longest request)
```

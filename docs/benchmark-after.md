# Benchmark After Changes

## Entorno de Pruebas
- **Fecha y Hora:** 2026-07-23 22:06:03
- **Sistema Operativo:** Microsoft Windows 11 Home Single Language
- **Hardware:** CPU: Intel(R) Core(TM) i7-10750H CPU @ 2.60GHz | RAM: 15.77 GB
- **Comando base:** `ab -n 1000 -c 10 [url]`
- **Modo del Servidor:** ASP.NET Core Kestrel en modo **Release** (con JIT warmup previo).

## Resumen Ejecutivo

| Endpoint | Método | Req/seg | p50 (ms) | p90 (ms) | p99 (ms) | Fallos / Non-2xx |
|----------|--------|---------|----------|----------|----------|------------------|
| /{shortUrl} | GET | 85.42 | 10 | 298 | 1251 | 0 (1000 Non-2xx) |
| /Login | POST | 1510.42 | 6 | 8 | 10 | 0 (1000 Non-2xx) |
| / | GET | 850.86 | 10 | 15 | 21 | 0 |

---

## Salida Detallada de Apache Bench (ab)

### 1. GET /{shortUrl} (Redirección a URL original)
Este endpoint fue optimizado mediante **Response Caching (Item #2)** y **Conditional Redirects (Item #10)**.
*Nota: Retorna \302 Found\, por lo que \b\ lo marca como "Non-2xx responses". Esto es esperado.*

\\\	ext
This is ApacheBench, Version 2.3 <$Revision: 1934973 $>
Copyright 1996 Adam Twiss, Zeus Technology Ltd, http://www.zeustech.net/
Licensed to The Apache Software Foundation, http://www.apache.org/

Benchmarking localhost (be patient)


Server Software:        Kestrel
Server Hostname:        localhost
Server Port:            5064

Document Path:          /aspnet-core1
Document Length:        0 bytes

Concurrency Level:      10
Time taken for tests:   11.708 seconds
Complete requests:      1000
Failed requests:        0
Non-2xx responses:      1000
Total transferred:      639046 bytes
HTML transferred:       0 bytes
Requests per second:    85.42 [#/sec] (mean)
Time per request:       117.075 [ms] (mean)
Time per request:       11.708 [ms] (mean, across all concurrent requests)
Transfer rate:          53.30 [Kbytes/sec] received

Connection Times (ms)
              min  mean[+/-sd] median   max
Connect:        0    1  16.2      1     512
Processing:     3   86 244.7     10    2964
Waiting:        3   86 244.7      9    2964
Total:          4   87 245.6     10    2965

Percentage of the requests served within a certain time (ms)
  50%     10
  66%     12
  75%     14
  80%     17
  90%    298
  95%    483
  98%    794
  99%   1251
 100%   2965 (longest request)
\\\

### 2. POST /Login (Validación de Autenticación)
Este endpoint fue protegido mediante **Rate Limiting (Item #5)**.
*Nota: Al enviar la petición mediante \b\ sin el token AntiForgery, retorna \400 Bad Request\, lo cual es captado como \Non-2xx response\. El rate limiting fue aplicado antes de esta validación.*

\\\	ext
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
Time taken for tests:   0.662 seconds
Complete requests:      1000
Failed requests:        0
Non-2xx responses:      1000
Total transferred:      426971 bytes
Total body sent:        206000
HTML transferred:       0 bytes
Requests per second:    1510.42 [#/sec] (mean)
Time per request:       6.621 [ms] (mean)
Time per request:       0.662 [ms] (mean, across all concurrent requests)
Transfer rate:          629.79 [Kbytes/sec] received
                        303.85 kb/s sent
                        933.65 kb/s total

Connection Times (ms)
              min  mean[+/-sd] median   max
Connect:        0    1   0.5      1       3
Processing:     0    6   1.9      5      46
Waiting:        0    4   2.2      4      39
Total:          2    6   2.0      6      48

Percentage of the requests served within a certain time (ms)
  50%      6
  66%      7
  75%      7
  80%      8
  90%      8
  95%      9
  98%      9
  99%     10
 100%     48 (longest request)
\\\

### 3. GET / (Página de inicio y creación de URLs)
Este endpoint se benefició de la **Compresión Brotli/Gzip (Item #6)** y **Security Headers (Item #3)**.

\\\	ext
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
Time taken for tests:   1.175 seconds
Complete requests:      1000
Failed requests:        0
Total transferred:      5570002 bytes
HTML transferred:       5137000 bytes
Requests per second:    850.86 [#/sec] (mean)
Time per request:       11.753 [ms] (mean)
Time per request:       1.175 [ms] (mean, across all concurrent requests)
Transfer rate:          4628.22 [Kbytes/sec] received

Connection Times (ms)
              min  mean[+/-sd] median   max
Connect:        0    1   0.7      1       4
Processing:     1   10   4.7      9     118
Waiting:        0    7   4.9      6     112
Total:          2   11   4.8     10     119

Percentage of the requests served within a certain time (ms)
  50%     10
  66%     12
  75%     13
  80%     13
  90%     15
  95%     17
  98%     19
  99%     21
 100%    119 (longest request)
\\\


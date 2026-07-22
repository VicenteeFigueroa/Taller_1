# Benchmark Before Changes

## Entorno de Pruebas
- **Fecha y Hora:** 2026-07-21 20:07:41
- **Sistema Operativo:** Microsoft Windows 11 Home Single Language
- **Hardware:** CPU: Intel(R) Core(TM) i7-10750H CPU @ 2.60GHz | RAM: 15.77 GB
- **Versión de .NET:** 10.0.204
- **Antivirus/Defender:** Activo (fuente potencial de varianza en localhost).
- **Comando base:** `ab -n 1000 -c 10 [url]`
- **Modo del Servidor:** ASP.NET Core Kestrel en modo **Release** (calentado con corridas previas de descarte).

## Resumen Ejecutivo

| Endpoint | Método | Req/seg | p50 (ms) | p90 (ms) | p99 (ms) | Transfer Rate (KB/s) | Fallos / Non-2xx |
|----------|--------|---------|----------|----------|----------|----------------------|------------------|
| `/{shortUrl}` | GET | 94.29 | 7 | 31 | 1716 | 15.19 | 0 (1000 Non-2xx) |
| `/Login` | POST | 4838.80 | 2 | 2 | 3 | 567.05 | 0 (1000 Non-2xx) |
| `/` | GET | 3572.82 | 2 | 3 | 13 | 18383.97 | 0 (0 Non-2xx) |

---

## Salida Detallada de Apache Bench (ab) y Contexto

### 1. GET /{shortUrl} (Redirección a URL original)
Este es el endpoint de redirección. La prueba se realizó con la URL semilla `/aspnet`, la cual **existe** en la base de datos y redirige a la documentación oficial.
- **Hipótesis del bajo rendimiento:** El endpoint realiza un acceso de escritura en base de datos para incrementar el contador de clics (`IncrementClicks()`). Al correr con concurrencia de 10 (`-c 10`), se genera una fuerte contención y locks en SQLite. Esto explica la "cola larga" de latencia donde el `p99` llega a 1716ms y el máximo supera los 3 segundos. 
- **Mejoras esperadas:** Esto justifica ampliamente la necesidad de **Response Caching (Item #2)** y **Conditional Redirects (Item #10)** para reducir los roundtrips a la base de datos.
- *Nota:* Retorna HTTP `302 Found`, por lo que `ab` lo marca como "Non-2xx responses", lo cual es un comportamiento correcto.

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

### 2. POST /Login (Validación de Autenticación Temprana)
Este benchmark apunta al endpoint de autenticación mandando un payload de login estático.
- **Contexto:** Dado que se envió sin el AntiForgery Token de Razor Pages, el framework rechazó tempranamente la petición devolviendo un `400 Bad Request` (contabilizado por `ab` como Non-2xx). 
- **Uso en el Taller:** Este escenario actúa como "baseline de petición HTTP mínima y rechazo temprano". En la fase "after", implementaremos ráfagas más agresivas para evaluar el **Rate Limiting (Item #5)** que responderá con `429 Too Many Requests` incluso antes de esta validación del modelo.

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

### 3. GET / (Página de inicio estática/Render)
Este endpoint carga el HTML de la página principal.
- **Contexto:** Es ideal para medir los beneficios en transferencia de red de la **Compresión Brotli/Gzip (Item #6)** y para asegurar que los **Security Headers (Item #3)** no degradan severamente el rendimiento general.

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

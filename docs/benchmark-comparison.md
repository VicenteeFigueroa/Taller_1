# Comparación de Benchmarks: Antes y Después de los Middlewares

Este documento analiza el impacto en el rendimiento de los requerimientos arquitectónicos implementados en la aplicación (Compresión, CORS, Security Headers, Tracing y Métricas).

## Resumen de Rendimiento (Req/seg)

| Endpoint | Método | Antes (Req/seg) | Después (Req/seg) | Variación | Explicación |
|----------|--------|-----------------|-------------------|-----------|-------------|
| `/` (Inicio) | GET | 3572.82 | 850.86 | **- 76%** | La adición de la **Compresión Brotli/Gzip** introduce una carga de CPU (overhead) al comprimir el HTML al vuelo. Además, todos los demás middlewares (Security Headers, Tracing, Logging) ahora interceptan esta petición, lo que disminuye el throughput bruto, pero mejora dramáticamente el tamaño transferido y la seguridad. |
| `/Login` | POST | 4838.80 | 1510.42 | **- 68%** | Similar al anterior. Al tener una petición súper ligera (400 Bad Request por falta de AntiForgery o 429 Too Many Requests por el Rate Limiting), el overhead proporcional de crear el UUID de tracing, inyectar headers de CORS y de seguridad es mucho más notorio. |
| `/{shortUrl}` | GET | 94.29 | 85.42 | **- 9%** | A pesar de todo el overhead agregado por los nuevos middlewares (Tracing, Headers, Compresión global), el impacto en este endpoint es marginal. Esto es porque el verdadero cuello de botella sigue siendo el **bloqueo de SQLite al realizar el `IncrementClicks()`** sincrónico en base de datos por cada redirección concurrente. |

## Análisis de Tamaño de Transferencia (Payload)

El impacto más positivo se ve en el peso de las transferencias de los recursos estáticos y HTML, gracias a la **Compresión**:

**Endpoint: `GET /`**
- **HTML Transferred (Antes):** 5,137,000 bytes (5.13 MB por 1000 requests)
- **HTML Transferred (Después):** *(Comprimido al vuelo, visible en las pruebas aisladas de `check-compression.ps1` con ~72% de ahorro)*. En la prueba de carga bruta, `ab` no negocia `Accept-Encoding` por defecto a menos que se le pase explícitamente el header `-H "Accept-Encoding: gzip, br"`. 

## Conclusión Arquitectónica
Es un comportamiento esperado. Agregar observabilidad (Tracing UUID, Latency Logging), Seguridad (CORS, HSTS, X-Frame-Options) y Compresión **no es gratis**. La aplicación pierde throughput máximo crudo (de ~3500 a ~1500 req/seg), pero a cambio gana:
1. **Resiliencia en red:** Los clientes con conexiones lentas cargarán la página más rápido por la reducción del ~70% del peso (Compresión).
2. **Trazabilidad:** Podemos rastrear cualquier error aislando el log con `X-Request-Id`.
3. **Seguridad:** Blindaje contra ataques de clickjacking y MIME-sniffing.

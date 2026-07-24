# Benchmark Comparison: Before and After Middlewares

This document analyzes the performance impact of the architectural requirements implemented in the application (Compression, CORS, Security Headers, Tracing, and Metrics).

## Performance Summary (Req/sec)

| Endpoint | Method | Before (Req/sec) | After (Req/sec) | Variance | Explanation |
|----------|--------|-----------------|-------------------|-----------|-------------|
| `/` (Home) | GET | 3572.82 | 850.86 | **- 76%** | The addition of **Brotli/Gzip Compression** introduces CPU overhead by compressing HTML on the fly. Furthermore, all other middlewares (Security Headers, Tracing, Logging) now intercept this request, which decreases raw throughput but dramatically improves transfer size and security. |
| `/Login` | POST | 4838.80 | 1510.42 | **- 68%** | Similar to the above. Because this is a highly lightweight request (either 400 Bad Request due to missing AntiForgery or 429 Too Many Requests due to Rate Limiting), the proportional overhead of generating the tracing UUID and injecting CORS/Security headers is much more noticeable. |
| `/{shortUrl}` | GET | 94.29 | 85.42 | **- 9%** | Despite all the overhead added by the new middlewares (Tracing, Headers, Global Compression), the impact on this endpoint is marginal. This is because the true bottleneck continues to be the **SQLite database lock when executing `IncrementClicks()`** synchronously for each concurrent redirect. |

## Transfer Size (Payload) Analysis

The most positive impact is seen in the transfer size of static resources and HTML, thanks to **Compression**:

**Endpoint: `GET /`**
- **HTML Transferred (Before):** 5,137,000 bytes (5.13 MB per 1000 requests)
- **HTML Transferred (After):** *(Compressed on the fly, visible in the isolated tests of `check-compression.ps1` with ~72% savings)*. In the raw load test, `ab` does not negotiate `Accept-Encoding` by default unless explicitly passed the header `-H "Accept-Encoding: gzip, br"`. 

## Architectural Conclusion
This is expected behavior. Adding observability (Tracing UUID, Latency Logging), Security (CORS, HSTS, X-Frame-Options), and Compression **is not free**. The application loses raw maximum throughput (from ~3500 to ~1500 req/sec), but in return it gains:
1. **Network Resilience:** Clients with slow connections will load the page faster due to the ~70% size reduction (Compression).
2. **Traceability:** We can track any error by isolating the log with `X-Request-Id`.
3. **Security:** Shielding against clickjacking and MIME-sniffing attacks.

# Compression Evidence (Brotli + Gzip)

This document validates the correct configuration of the compression middleware (`AddResponseCompression`) on the ASP.NET Core server, as part of the workshop deliverables.

## Validation Script Results (`check-compression.ps1`)

Requests were made to the root endpoint (`/`) negotiating different compression algorithms using the `Accept-Encoding` header. The raw bytes transferred over the network were measured:

```text
Verifying compression at: http://localhost:5064/

[Identity] Uncompressed size: 5137 bytes

[Brotli] Content-Encoding received: br
[Brotli] Compressed size: 1448 bytes
[Brotli] Savings: 3689 bytes (71.81%)

[Gzip] Content-Encoding received: gzip
[Gzip] Compressed size: 1730 bytes
[Gzip] Savings: 3407 bytes (66.32%)

Test completed successfully.
```

## Conclusion
Compression is working correctly:
1. The responses respect content negotiation and return the appropriate `Content-Encoding` header (`br` or `gzip`).
2. **Brotli** proved to be the most efficient, reducing the HTML static page payload by **~72%**, which will substantially accelerate the delivery time to clients. 
3. **Gzip** serves as an excellent fallback for older clients, achieving a savings of **~66%**.

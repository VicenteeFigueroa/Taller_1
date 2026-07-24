# Request Tracing (X-Request-Id)

This document details the implementation of request traceability using the `X-Request-Id` header and log correlation using Serilog.

## Implementation
The `RequestTracingMiddleware` was created to intercept all requests at the beginning of the pipeline (`Program.cs`). 
Its function is:
1. Check if the client already sent an `X-Request-Id`. If not, it uses the native ASP.NET Core `TraceIdentifier` or generates a new UUID.
2. Attach this ID as an `X-Request-Id` header in the response (useful for the client to save if they need to report a support error).
3. Enrich the Serilog context (`LogContext.PushProperty`) so that **all** logs generated during the lifespan of that request include this ID.

The `outputTemplate` in `appsettings.json` was configured so that console logs explicitly expose the field:
`{Timestamp:yyyy-MM-dd HH:mm:ss,fff} [{Level:u8}] [ReqId={RequestId}] ...`

## Validation and Examples
When making a request to the server (e.g., a non-existent link `/missing-link` that triggers a failed redirect and EF Core error logs), we can observe that the request ID propagates through all application events uniformly:

```text
2026-07-23 20:37:32,654 [DEBUG] [ReqId=0HNN93EVU0NVU:00000001] Shortly.Application.Services.LinkService ... - Retrieving link with shortUrl: missing-link
2026-07-23 20:37:32,801 [ERROR] [ReqId=0HNN93EVU0NVU:00000001] Microsoft.EntityFrameworkCore.Database.Command ... - Failed executing DbCommand
2026-07-23 20:37:32,815 [ERROR] [ReqId=0HNN93EVU0NVU:00000001] Microsoft.EntityFrameworkCore.Query ... - An exception occurred while iterating over the results of a query
```

This exact RequestID (`0HNN93EVU0NVU:00000001`) will be returned to the client in the HTTP `X-Request-Id` response header.

## How to Use It for Diagnostics?
If a user reports that a URL is not redirecting or that the platform is slow:
1. Ask them to open the developer tools in their browser (Network Tab) and share the `X-Request-Id` header of the failed request.
2. Filter the logs on your server looking exclusively for that ID (e.g., using grep, or in centralized logging tools like Kibana, Datadog, or Seq: `RequestId = '0HNN93EVU0NVU:00000001'`).
3. This will give you the exact execution thread of *that* user isolated from the noise of the rest of the traffic.

using Serilog.Context;

namespace Shortly.Middlewares;

public class RequestTracingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestTracingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Create or reuse a unique identifier for the request
        var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault() 
                        ?? context.TraceIdentifier 
                        ?? Guid.NewGuid().ToString();

        // Append the Request ID to the response headers
        context.Response.Headers.Append("X-Request-Id", requestId);

        // Enrich the Serilog context with this RequestId.
        // Any logs written during the downstream execution will include this property.
        using (LogContext.PushProperty("RequestId", requestId))
        {
            await _next(context);
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Shortly.Middlewares
{
    public class PerformanceMeasurementMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMeasurementMiddleware> _logger;

        public PerformanceMeasurementMiddleware(RequestDelegate next, ILogger<PerformanceMeasurementMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // We must add the header before the response body starts sending
            context.Response.OnStarting(() =>
            {
                // Note: This measures the time up to the point where headers are sent.
                var elapsedHeadersMs = stopwatch.ElapsedMilliseconds;
                context.Response.Headers.Append("X-Response-Time", $"{elapsedHeadersMs}ms");
                return Task.CompletedTask;
            });

            await _next(context);

            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Goal: Emit dedicated slow-request logs for > 500ms
            if (elapsedMs > 500)
            {
                _logger.LogWarning(
                    "SLOW_REQUEST_DETECTED | Method: {Method} | Path: {Path} | Status: {StatusCode} | Elapsed: {Elapsed}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    elapsedMs);
            }
        }
    }
}

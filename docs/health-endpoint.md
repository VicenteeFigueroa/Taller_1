# Liveness Health Check Endpoint

This document describes the configuration of the health check endpoint implemented in the application, designed for integration with monitoring systems or load balancers (such as Kubernetes, Docker Swarm, or AWS ALB).

## Implementation (`Program.cs`)

The native .NET middleware `AddHealthChecks()` was enabled, and a route at `/health` was configured with a custom `ResponseWriter`. 
The logic captures the exact moment the application starts (`DateTimeOffset.UtcNow` at the beginning of `Program.cs`) and calculates the elapsed time to report it as `uptime`.

## Response Payload

The endpoint was tested by making successive calls to verify that the server starts correctly and the uptime increases.

**Example response (Status 200 OK):**
```json
{
  "status": "Healthy",
  "uptime": "00:00:03.2236346",
  "timestamp": "2026-07-24T00:57:43.0601925+00:00"
}
```

### Justification of Fields
- **`status`**: Indicates if the application is responding. Currently, it always returns `Healthy` since it serves as a *liveness* probe (if the process does not respond, the request will time out and the orchestrator will know the application died).
- **`uptime`**: Exposes the continuous uptime of the process in the format `d.hh:mm:ss.fffffff`. Extremely useful for detecting *CrashLoops* (constant application restarts) from the monitoring system.
- **`timestamp`**: Serves to confirm that the response is not a stuck cache and that the server is generating dynamic responses in real time.

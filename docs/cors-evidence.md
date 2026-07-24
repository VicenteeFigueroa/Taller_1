# Restrictive CORS Configuration Evidence

This document validates the correct configuration of the CORS (Cross-Origin Resource Sharing) policy on the ASP.NET Core server, fulfilling the requirement to allow only trusted origins.

## Implemented Configuration (`Program.cs`)
The policy was applied with the minimum necessary scope using `UseCors` with the following explicit rules:
- **Allowed origins:** Only `https://trusted.shortly.com`
- **Allowed methods:** Only `GET` and `POST`
- **Allowed headers:** Only `Content-Type` and `Authorization`

## Preflight Flow (`OPTIONS` Request)

When a browser attempts to make a complex cross-origin request (e.g., a `POST` with `Content-Type: application/json`), it first sends a preliminary `OPTIONS` request (Preflight request) to ask the server if the operation is permitted.

### 1. Test with Allowed Origin (`https://trusted.shortly.com`)
The `OPTIONS` request was simulated by indicating the trusted origin. The server responded by explicitly returning the `Access-Control-Allow-*` headers:

```text
--- ALLOWED ORIGIN TEST ---
Origin: https://trusted.shortly.com
Status: NoContent
Access-Control-Allow-Origin: https://trusted.shortly.com
Access-Control-Allow-Methods: GET,POST
Access-Control-Allow-Headers: Content-Type,Authorization
```
**Effect:** The browser sees these headers, confirms they match its origin and intent, and proceeds to send the actual `POST` request.

### 2. Test with Denied Origin (`https://evil.hacker.com`)
The same `OPTIONS` request was simulated but from an unauthorized origin. The middleware processes the request but deliberately omits injecting the CORS headers:

```text
--- DENIED ORIGIN TEST ---
Origin: https://evil.hacker.com
Status: NoContent
Access-Control-Allow-Origin: (Not present, CORS blocked by the browser)
```
**Effect:** The browser receives a `204` status, but finding no `Access-Control-Allow-Origin` header, it assumes the server rejects the origin and immediately aborts the connection, protecting the user.

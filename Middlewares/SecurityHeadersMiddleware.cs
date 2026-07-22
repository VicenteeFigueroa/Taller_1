using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Shortly.Middlewares
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Adding security headers to every response globally

            // 1. Strict-Transport-Security (HSTS)
            // Mitigates: Man-in-the-Middle (MitM) attacks and protocol downgrade attacks.
            // Action: Forces the browser to only communicate with the server over HTTPS.
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

            // 2. X-Content-Type-Options
            // Mitigates: MIME-sniffing attacks (where browsers guess the content type and execute malicious scripts disguised as images/text).
            // Action: Forces the browser to trust the Content-Type header strictly.
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // 3. X-Frame-Options
            // Mitigates: Clickjacking attacks (where an attacker embeds your site in an invisible iframe to trick users into clicking).
            // Action: Denies rendering this site inside a frame/iframe on other domains.
            context.Response.Headers.Append("X-Frame-Options", "DENY");

            // 4. Referrer-Policy
            // Mitigates: Information leakage via the Referer header (e.g., leaking sensitive tokens in the URL to third-party sites).
            // Action: Only sends the referrer origin on same-origin requests, strips it on cross-origin.
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // 5. Permissions-Policy
            // Mitigates: Malicious use of browser features (like camera, microphone, geolocation) by compromised or third-party scripts.
            // Action: Disables access to sensitive APIs unless explicitly allowed.
            context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

            await _next(context);
        }
    }
}

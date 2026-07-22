using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Shortly.Application.Interfaces;

namespace Shortly.Endpoints;

public static class UrlRedirectEndpoint
{
    public static void MapUrlRedirect(this WebApplication app)
    {
        app.MapGet("/{shortUrl}", async (string shortUrl, HttpRequest request, HttpResponse response,
            ILinkService linkService) =>
        {
            try
            {
                var link = await linkService.GetLink(shortUrl);

                // Compute the ETag and Last-Modified headers based on the link's stable state.
                var etag = ComputeETag(link.Id, link.ShortUrl, link.Url, link.CreatedAtUtc);
                var lastModified = new DateTimeOffset(link.CreatedAtUtc, TimeSpan.Zero);

                // Cache control headers: allow caching, but require revalidation.
                // This ensures that the client will check with the server if the cached
                // response is still valid, preventing stale data from being served.
                response.Headers.CacheControl = "public, max-age=60, must-revalidate";
                response.Headers.ETag = etag;
                response.Headers.LastModified = lastModified.ToString("R", CultureInfo.InvariantCulture);

                // Clicks still counts every real hit the server processes, cache-validated or not —
                // that's a business metric independent of whether we saved bandwidth on the response body.
                await linkService.IncrementClicks(link.Id);

                if (IsNotModified(request, etag, lastModified))
                {
                    return Results.StatusCode(StatusCodes.Status304NotModified);
                }

                return Results.Redirect(link.Url);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        });
    }


    /// Per RFC 7232 §6, If-None-Match takes precedence over If-Modified-Since when both are
    /// sent: an ETag match (or "*") is decisive, and If-Modified-Since is only consulted when
    /// no If-None-Match header was sent at all.
    
    private static bool IsNotModified(HttpRequest request, string etag, DateTimeOffset lastModified)
    {
        var ifNoneMatch = request.Headers.IfNoneMatch;
        if (ifNoneMatch.Count > 0)
        {
            return ifNoneMatch.Any(value => value == "*" || value == etag);
        }

        var ifModifiedSinceHeader = request.Headers.IfModifiedSince.ToString();
        if (!string.IsNullOrEmpty(ifModifiedSinceHeader) &&
            DateTimeOffset.TryParse(ifModifiedSinceHeader, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var ifModifiedSince))
        {
            // HTTP-dates only carry second precision, so truncate our side to match before comparing.
            var truncatedLastModified = new DateTimeOffset(
                lastModified.Year, lastModified.Month, lastModified.Day,
                lastModified.Hour, lastModified.Minute, lastModified.Second, lastModified.Offset);

            return truncatedLastModified <= ifModifiedSince;
        }

        return false;
    }


    /// Strong ETag: a SHA-256 hash of the link's immutable fields, hex-encoded and quoted per
    /// RFC 7232 §2.3. Deterministic for a given link state, so it only changes if the
    /// underlying data the client actually cares about (the redirect target) changes.
 
    private static string ComputeETag(long id, string shortUrl, string url, DateTime createdAtUtc)
    {
        var raw = $"{id}|{shortUrl}|{url}|{createdAtUtc:O}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return $"\"{hex}\"";
    }
}
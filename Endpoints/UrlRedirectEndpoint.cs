using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Shortly.Application.DTOs;
using Shortly.Application.Interfaces;

namespace Shortly.Endpoints;

public static class UrlRedirectEndpoint
{
    public static void MapUrlRedirect(this WebApplication app)
    {
        app.MapGet("/{shortUrl}", async (string shortUrl, HttpRequest request, HttpResponse response,
            ILinkService linkService) =>
        {
            // 400: the requested shortUrl isn't even shaped like one of ours (wrong length or
            // characters outside our URL-safe base64 alphabet). This is a client error distinct
            // from "not found" — the request itself is malformed, not just pointing at a
            // resource that happens not to exist, so 400 is the semantically correct code
            // rather than 404.
            if (!ShortUrlFormat.IsMatch(shortUrl))
            {
                return Results.Problem(
                    title: "Malformed short code",
                    detail: $"'{shortUrl}' is not a validly-formatted short code. Expected exactly " +
                            "12 characters from the URL-safe base64 alphabet (a-z, 0-9, '-', '_').",
                    statusCode: StatusCodes.Status400BadRequest,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.1");
            }

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

                var (permanent, preserveMethod) = DecideRedirectSemantics(link);
                return Results.Redirect(link.Url, permanent, preserveMethod);
            }
            catch (KeyNotFoundException)
            {
                // 404: well-formed short code, but no link is registered under it. Results.Problem
                // produces an RFC 7807 "application/problem+json" body (title/status/detail/type)
                // instead of an empty 404, so API clients get a machine-readable reason instead of
                // having to guess from the status code alone.
                return Results.Problem(
                    title: "Short link not found",
                    detail: $"No link is registered for short code '{shortUrl}'.",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.4");
            }
        });
    }

    /// <summary>
    /// Matches the exact shape GenerateShortUrl() in LinkService produces: 12 characters from
    /// the URL-safe base64 alphabet, lowercased. Anything outside this shape cannot possibly be
    /// a real short code, so it's rejected as a 400 before ever touching the database.
    /// </summary>
    private static readonly Regex ShortUrlFormat = new("^[a-z0-9_-]{12}$", RegexOptions.Compiled);

    /// Picks which of 301/302/307 semantically matches this link's actual permanence, instead
    /// of always answering 302. Results.Redirect's (permanent, preserveMethod) pair maps
    /// directly onto the four HTTP redirect codes:
    ///   permanent=false, preserveMethod=false -> 302 Found
    ///   permanent=false, preserveMethod=true  -> 307 Temporary Redirect
    ///   permanent=true,  preserveMethod=false -> 301 Moved Permanently
    ///   permanent=true,  preserveMethod=true  -> 308 Permanent Redirect (not produced here, but
    ///                                            it's the "corrected 301" the same way 307 is
    ///                                            the "corrected 302" — see the table below)
    ///
    /// Decision order:
    ///  1) An explicit ExpiresAtUtc makes a link temporary by definition — its target or
    ///     validity can change at any moment, so clients/caches must always come back and
    ///     re-check rather than memorize the mapping. 307 also guarantees the original method
    ///     and body survive the redirect, which matters for temporary/expiring resources more
    ///     than for a simple permanent alias.
    ///  2) A stable (non-expiring) link that has proven popular (>100 accesses) is treated as
    ///     permanent enough that clients/caches are authorized to remember it indefinitely —
    ///     that authorization is exactly what 301 grants.
    ///  3) Everything else: an ordinary, still-young link with no stated permanence guarantee
    ///     gets the historically-default 302 — cacheable only if explicit caching headers say
    ///     so (which ours do, via ETag/Last-Modified), never assumed by the client on its own.

    private static (bool Permanent, bool PreserveMethod) DecideRedirectSemantics(LinkResponse link)
    {
        if (link.ExpiresAtUtc is not null)
        {
            return (Permanent: false, PreserveMethod: true); // 307
        }

        if (link.Clicks > 100)
        {
            return (Permanent: true, PreserveMethod: false); // 301
        }

        return (Permanent: false, PreserveMethod: false); // 302
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
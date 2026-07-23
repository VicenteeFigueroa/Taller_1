using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shortly.Domain.Entities;

[Table("links")]
[Index(nameof(ShortUrl), IsUnique = true)]
public class Link
{
    [Key]
    public long Id { get; private set; }

    [Required]
    [MaxLength(20248)]
    public string Url { get; private set; } = null!;

    [Required]
    [MaxLength(32)]
    public string ShortUrl { get; private set; } = null!;

    [Required] public int Clicks { get; private set; }

    
    /// Set once at creation and never modified afterwards. Used as the stable "last modified"
    /// timestamp for HTTP caching validators (see UrlRedirectEndpoint) — unlike Clicks, which
    /// changes on almost every request, this value stays constant for the link's lifetime.
   
    [Required]
    public DateTime CreatedAtUtc { get; private set; }

    /// Null for a normal, indefinitely-lived link. When set, marks the link as temporary/expiring —
    /// used by UrlRedirectEndpoint to decide it must always be redirected with 307 (never cached
    /// as permanent), regardless of how many clicks it has accumulated.
    public DateTime? ExpiresAtUtc { get; private set; }

    [ForeignKey(nameof(User))]
    public long UserId { get; private set; }

    public User User { get; private set; } = null!;

    private Link()
    {
    }

    public Link(string url, string shortUrl, long userId, DateTime? expiresAtUtc = null)
    {
        Url = string.IsNullOrWhiteSpace(url)
            ? throw new ArgumentException("URL is required.", nameof(url))
            : url.Trim();

        ShortUrl = string.IsNullOrWhiteSpace(shortUrl)
            ? throw new ArgumentException("ShortUrl is required.", nameof(shortUrl))
            : shortUrl.Trim();

        UserId = userId > 0
            ? userId
            : throw new ArgumentOutOfRangeException(nameof(userId), "UserId must be greater than zero.");

        Clicks = 0;
        CreatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void IncrementClicks() => Clicks++;

}
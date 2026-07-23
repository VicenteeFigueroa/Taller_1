using Shortly.Domain.Entities;

namespace Shortly.Application.DTOs;

public class LinkResponse
{
    public long Id { get; init; }
    public string Url { get; init; } = null!;
    public string ShortUrl { get; init; } = null!;
    public int Clicks { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public static LinkResponse From(Link link) => new()
    {
        Id = link.Id,
        Url = link.Url,
        ShortUrl = link.ShortUrl,
        Clicks = link.Clicks,
        CreatedAtUtc = link.CreatedAtUtc,
        ExpiresAtUtc = link.ExpiresAtUtc
    };
}
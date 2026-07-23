using Shortly.Application.DTOs;

namespace Shortly.Application.Interfaces;

public interface ILinkService
{
    Task<LinkResponse> CreateLink(string url, long userId, DateTime? expiresAtUtc = null);

    Task<LinkResponse> IncrementClicks(long linkId);

    Task<LinkResponse> GetLink(string shortUrl);

    Task<List<LinkResponse>> GetAllLinks();

    Task<List<LinkResponse>> GetLinksByUserId(long userId);
}
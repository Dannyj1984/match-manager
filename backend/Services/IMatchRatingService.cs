using FairPlay.Api.DTOs;

namespace FairPlay.Api.Services;

public interface IMatchRatingService
{
    Task SaveMatchRatingsAsync(Guid matchId, Guid raterId, List<RatingSubmissionDto> ratings);
    Task<decimal?> CalculateAvgMatchRatingAsync(Guid playerId);
}

using FairPlay.Api.Data;
using FairPlay.Api.Models;
using FairPlay.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FairPlay.Api.Services;

public class MatchRatingService : IMatchRatingService
{
    private readonly FairPlayDbContext _context;

    public MatchRatingService(FairPlayDbContext context)
    {
        _context = context;
    }

    public async Task SaveMatchRatingsAsync(Guid matchId, Guid raterId, List<RatingSubmissionDto> ratings)
    {
        // Remove any existing ratings from this rater for this match (allow re-rating)
        var existingRatings = await _context.PlayerRatings
            .Where(pr => pr.MatchId == matchId && pr.RaterId == raterId)
            .ToListAsync();
        
        _context.PlayerRatings.RemoveRange(existingRatings);

        // Add new ratings
        foreach (var rating in ratings)
        {
            _context.PlayerRatings.Add(new PlayerRating
            {
                Id = Guid.NewGuid(),
                MatchId = matchId,
                RaterId = raterId,
                RatedPlayerId = rating.SubjectId,
                Rating = rating.Value,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        // Recalculate average for all rated players
        foreach (var rating in ratings)
        {
            await UpdatePlayerAvgRatingAsync(rating.SubjectId);
        }
    }

    public async Task<decimal?> CalculateAvgMatchRatingAsync(Guid playerId)
    {
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

        var ratings = await _context.PlayerRatings
            .Where(pr => pr.RatedPlayerId == playerId && pr.CreatedAt >= sixMonthsAgo)
            .Select(pr => pr.Rating)
            .ToListAsync();

        if (ratings.Count == 0)
            return null;

        return (decimal)ratings.Average();
    }

    private async Task UpdatePlayerAvgRatingAsync(Guid playerId)
    {
        var player = await _context.Players.FindAsync(playerId);
        if (player == null) return;

        player.AvgMatchRating = await CalculateAvgMatchRatingAsync(playerId);
        await _context.SaveChangesAsync();
    }
}

using FairPlay.Api.Data;
using FairPlay.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FairPlay.Api.Services;

public interface IRatingUpdateService
{
    Task CompleteMatchAsync(Guid matchId);
}

public class RatingUpdateService : IRatingUpdateService
{
    private readonly FairPlayDbContext _context;

    public RatingUpdateService(FairPlayDbContext context)
    {
        _context = context;
    }

    public Task CompleteMatchAsync(Guid matchId)
    {
        return _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var match = await _context.Matches
                    .Include(m => m.RawRatings)
                    .Include(m => m.MatchAssignments)
                    .ThenInclude(ma => ma.Player)
                    .FirstOrDefaultAsync(m => m.Id == matchId);

                if (match == null || match.IsCompleted) return;

                // Calculate average rating per subject for this match
                var subjectAverages = match.RawRatings
                    .GroupBy(r => r.SubjectId)
                    .Select(g => new
                    {
                        PlayerId = g.Key,
                        AverageRating = (decimal)g.Average(r => r.Value)
                    })
                    .ToDictionary(x => x.PlayerId, x => x.AverageRating);

                foreach (var assignment in match.MatchAssignments)
                {
                    var player = assignment.Player;
                    if (subjectAverages.TryGetValue(player.Id, out var matchAvg))
                    {
                        // Formula: New = (Old * 0.8) + (MatchAvg * 0.2)
                        player.CurrentRating = (player.CurrentRating * 0.8m) + (matchAvg * 0.2m);
                    }
                    
                    player.LastPlayed = match.Date;
                }

                match.IsCompleted = true;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}

using FairPlay.Api.Data;
using FairPlay.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FairPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaderboardController : ControllerBase
{
    private readonly FairPlayDbContext _context;

    public LeaderboardController(FairPlayDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [LeagueContext]
    public async Task<IActionResult> GetLeaderboard([FromQuery] string timeframe = "all")
    {
        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        
        // Base query: all ratings for completed matches in this league
        var query = _context.PlayerRatings
            .Include(r => r.Match)
            .Where(r => r.Match!.LeagueId == leagueId && r.Match.IsCompleted);

        // Apply Timeframe Filters
        var today = DateTime.UtcNow.Date;
        if (timeframe == "3m")
        {
            var cutoff = today.AddMonths(-3);
            query = query.Where(r => r.Match!.Date >= cutoff);
        }
        else if (timeframe == "6m")
        {
            var cutoff = today.AddMonths(-6);
            query = query.Where(r => r.Match!.Date >= cutoff);
        }
        else if (timeframe == "12m")
        {
            var cutoff = today.AddMonths(-12);
            query = query.Where(r => r.Match!.Date >= cutoff);
        }

        // Aggregate Data
        // Note: EF Core GroupBy translation can be limited. 
        // We select the raw data points we need first to ensure efficiency.
        var rawData = await query
            .Select(r => new { r.RatedPlayerId, r.Rating, r.MatchId })
            .ToListAsync();

        // Perform grouping in memory to avoid complex SQL translation issues
        var stats = rawData
            .GroupBy(r => r.RatedPlayerId)
            .Select(g => new
            {
                PlayerId = g.Key,
                AverageRating = g.Average(r => (decimal)r.Rating),
                MatchesPlayed = g.Select(r => r.MatchId).Distinct().Count(),
                HighestRating = g.Max(r => (decimal)r.Rating)
            })
            // Minimum threshold: 3 matches to appear on leaderboard (prevents 1-match wonders)
            .Where(s => s.MatchesPlayed >= 3) 
            .OrderByDescending(s => s.AverageRating)
            .ThenByDescending(s => s.MatchesPlayed)
            .ToList();

        // Fetch Player Details (Names)
        var playerIds = stats.Select(s => s.PlayerId).Distinct().ToList();
        var players = await _context.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName);

        // Build Final DTO
        var leaderboard = stats.Select((s, index) => new
        {
            Rank = index + 1,
            PlayerId = s.PlayerId,
            Name = players.ContainsKey(s.PlayerId) ? players[s.PlayerId] : "Unknown",
            Rating = Math.Round(s.AverageRating, 1),
            MatchesPlayed = s.MatchesPlayed,
            HighestRating = s.HighestRating
        });

        return Ok(leaderboard);
    }
}

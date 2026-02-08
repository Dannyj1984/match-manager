using FairPlay.Api.Data;
using FairPlay.Api.Models;
using FairPlay.Api.Services;
using FairPlay.Api.DTOs;
using FairPlay.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FairPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MatchesController : ControllerBase
{
    private readonly FairPlayDbContext _context;
    private readonly ITeamBalancerService _balancer;
    private readonly IMatchRatingService _ratingService;

    public MatchesController(FairPlayDbContext context, ITeamBalancerService balancer, IMatchRatingService ratingService)
    {
        _context = context;
        _balancer = balancer;
        _ratingService = ratingService;
    }

    [HttpPost("calculate-teams")]
    [LeagueContext(restrictSuperAdmin: true), LeagueAdmin]
    public async Task<IActionResult> CalculateTeams([FromBody] TeamCalculationRequest request)
    {
        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        
        // Get league to determine sport
        var league = await _context.Leagues.FindAsync(leagueId);
        if (league == null) return NotFound("League not found");
        
        var players = await _context.Players
            .Where(p => p.LeagueId == leagueId && request.PlayerIds.Contains(p.Id))
            .ToListAsync();

        if (players.Count != request.PlayerIds.Count)
            return BadRequest("Some player IDs were not found.");

        var assignments = _balancer.BalanceTeams(Guid.Empty, players, request.TeamCount, league.Sport);
        return Ok(assignments.Select(a => new { a.PlayerId, a.TeamNumber }));
    }

    [HttpPatch("{id}/complete")]
    [LeagueContext(restrictSuperAdmin: true), LeagueAdmin]
    public async Task<IActionResult> CompleteMatch(Guid id)
    {
        var match = await _context.Matches.FindAsync(id);
        if (match == null) return NotFound();

        match.IsCompleted = true;
        await _context.SaveChangesAsync();

        // Note: Player avg match ratings are now updated when users submit match performance ratings
        // via the MatchRatingService, not automatically when match is completed

        return Ok(new { Message = "Match marked as completed" });
    }

    [HttpGet("by-date/{date}")]
    [LeagueContext(restrictSuperAdmin: true)]
    public async Task<IActionResult> GetMatchByDate(DateTime date)
    {
        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        
        var match = await _context.Matches
            .Include(m => m.MatchAssignments)
            .ThenInclude(ma => ma.Player)
            .Where(m => m.LeagueId == leagueId && m.Date.Date >= date.Date)
            .OrderBy(m => m.Date)
            .FirstOrDefaultAsync();

        if (match == null) return NotFound();
        return Ok(match);
    }

    [HttpPost]
    [LeagueContext(restrictSuperAdmin: true), LeagueAdmin]
    public async Task<IActionResult> CreateMatch([FromBody] CreateMatchRequest request)
    {
        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        
        // Get league for default location
        var league = await _context.Leagues.FindAsync(leagueId);
        
        // Standardize date
        var matchDate = request.Date.ToUniversalTime().Date;

        // Check for existing match
        var match = await _context.Matches
            .Include(m => m.MatchAssignments)
            .FirstOrDefaultAsync(m => m.LeagueId == leagueId && m.Date.Date == matchDate);

        if (match != null)
        {
            // Update existing match
            match.Location = request.Location ?? match.Location;
            match.FormatType = request.FormatType;
            
            // Update assignments
            // Remove existing assignments
            _context.MatchAssignments.RemoveRange(match.MatchAssignments);
            
            // Add new assignments
            foreach (var assignment in request.Assignments)
            {
                _context.MatchAssignments.Add(new MatchAssignment
                {
                    MatchId = match.Id,
                    PlayerId = assignment.PlayerId,
                    TeamNumber = assignment.TeamNumber
                });
            }
            
            await _context.SaveChangesAsync();
            return Ok(match);
        }
        else
        {
            // Create new match
            match = new Match
            {
                Id = Guid.NewGuid(),
                LeagueId = leagueId,
                Date = matchDate,
                Location = request.Location ?? league?.Location,
                FormatType = request.FormatType,
                IsCompleted = false
            };

            foreach (var assignment in request.Assignments)
            {
                match.MatchAssignments.Add(new MatchAssignment
                {
                    MatchId = match.Id,
                    PlayerId = assignment.PlayerId,
                    TeamNumber = assignment.TeamNumber
                });
            }

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetMatch), new { id = match.Id }, match);
        }
    }

    [HttpGet("{id}")]
    [LeagueContext(restrictSuperAdmin: true)]
    public async Task<IActionResult> GetMatch(Guid id)
    {
        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        
        var match = await _context.Matches
            .Include(m => m.MatchAssignments)
            .ThenInclude(ma => ma.Player)
            .Include(m => m.RawRatings)
            .FirstOrDefaultAsync(m => m.Id == id && m.LeagueId == leagueId);

        if (match == null) return NotFound();
        return Ok(match);
    }

    [HttpGet("dashboard")]
    [LeagueContext(required: false, restrictSuperAdmin: true)]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = User.FindFirstValue("userId");
        var leagueIdObj = HttpContext.Items["LeagueId"];
        
        // If no league context, return empty dashboard
        if (leagueIdObj == null)
        {
            return Ok(new
            {
                lastMatchDate = (DateTime?)null,
                nextMatch = (object?)null,
                recentMatches = new List<object>(),
                needsLeague = true
            });
        }
        
        var leagueId = (Guid)leagueIdObj;
        
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.IdentityUserId == userId && p.LeagueId == leagueId);
        if (player == null) return NotFound();

        // 1. Most recent completed match for the user
        var lastCompletedMatch = await _context.MatchAssignments
            .Include(ma => ma.Match)
            .Where(ma => ma.PlayerId == player.Id && ma.Match.IsCompleted)
            .OrderByDescending(ma => ma.Match.Date)
            .Select(ma => ma.Match.Date)
            .FirstOrDefaultAsync();

        // 2. Next active match (incomplete, in future or today)
        var nextActiveMatch = await _context.Matches
            .Where(m => !m.IsCompleted && m.Date.Date >= DateTime.UtcNow.Date)
            .OrderBy(m => m.Date)
            .Select(m => m.Date)
            .FirstOrDefaultAsync();

        // 3. Recent performance (average rating per match for the user, last 4 matches)
        var recentMatches = await _context.MatchAssignments
            .Include(ma => ma.Match)
            .Where(ma => ma.PlayerId == player.Id && ma.Match.IsCompleted)
            .OrderByDescending(ma => ma.Match.Date)
            .Take(4)
            .Select(ma => ma.MatchId)
            .ToListAsync();

        var performance = await _context.PlayerRatings
            .Where(r => r.RatedPlayerId == player.Id && recentMatches.Contains(r.MatchId))
            .GroupBy(r => r.MatchId)
            .Select(g => new { 
                Date = _context.Matches.Where(m => m.Id == g.Key).Select(m => m.Date).FirstOrDefault(),
                Value = Math.Round(g.Average(r => r.Rating), 1)
            })
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        // 4. Find latest completed match that user hasn't rated yet
        var matchesNeedingRatings = await _context.MatchAssignments
            .Include(ma => ma.Match)
            .Where(ma => ma.PlayerId == player.Id && ma.Match.IsCompleted)
            .Select(ma => ma.Match)
            .OrderByDescending(m => m.Date)
            .ToListAsync();

        DateTime? pendingRatingMatchDate = null;
        foreach (var match in matchesNeedingRatings)
        {
            var hasRated = await _context.PlayerRatings.AnyAsync(r => r.MatchId == match.Id && r.RaterId == player.Id);
            if (!hasRated)
            {
                pendingRatingMatchDate = match.Date;
                break;
            }
        }

        return Ok(new
        {
            LastCompletedMatchDate = lastCompletedMatch != default ? lastCompletedMatch.ToString("yyyy-MM-dd") : null,
            NextActiveMatchDate = nextActiveMatch != default ? nextActiveMatch.ToString("yyyy-MM-dd") : null,
            PendingRatingMatchDate = pendingRatingMatchDate?.ToString("yyyy-MM-dd"),
            RecentPerformance = performance
        });
    }

    [HttpPost("{id}/submit-ratings")]
    public async Task<IActionResult> SubmitRatings(Guid id, [FromBody] List<RatingSubmissionDto> ratings)
    {
        var userId = User.FindFirstValue("userId");
        var rater = await _context.Players.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
        if (rater == null) return Unauthorized();

        var match = await _context.Matches.Include(m => m.MatchAssignments).FirstOrDefaultAsync(m => m.Id == id);
        if (match == null) return NotFound();
        if (!match.IsCompleted) return BadRequest("Cannot rate players for an incomplete match.");

        // Check if rater was a participant in this match
        var wasParticipant = match.MatchAssignments.Any(ma => ma.PlayerId == rater.Id);
        if (!wasParticipant) return StatusCode(403, "Only players who participated in this match can submit ratings.");

        // Verify all subjects are in the match
        var validRatings = ratings.Where(r => match.MatchAssignments.Any(ma => ma.PlayerId == r.SubjectId)).ToList();

        // Use new rating service (allows re-rating, saves to PlayerRatings table, updates averages)
        await _ratingService.SaveMatchRatingsAsync(id, rater.Id, validRatings);

        return Ok(new { Message = "Ratings submitted successfully" });
    }

    [HttpGet("{id}/my-ratings")]
    public async Task<IActionResult> GetMyRatings(Guid id)
    {
        var userId = User.FindFirstValue("userId");
        var rater = await _context.Players.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
        if (rater == null) return Unauthorized();

        var ratings = await _context.PlayerRatings
            .Where(r => r.MatchId == id && r.RaterId == rater.Id)
            .Select(r => new { SubjectId = r.RatedPlayerId, Value = r.Rating })
            .ToListAsync();

        return Ok(ratings);
    }

    [HttpGet("{id}/can-rate")]
    public async Task<IActionResult> CanRateMatch(Guid id)
    {
        var userId = User.FindFirstValue("userId");
        var player = await _context.Players.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
        if (player == null) return Unauthorized();

        var match = await _context.Matches.Include(m => m.MatchAssignments).FirstOrDefaultAsync(m => m.Id == id);
        if (match == null) return NotFound();

        var wasParticipant = match.MatchAssignments.Any(ma => ma.PlayerId == player.Id);
        return Ok(new { canRate = wasParticipant && match.IsCompleted });
    }

    [HttpPost("toggle-participation")]
    [LeagueContext(restrictSuperAdmin: true)]
    public async Task<IActionResult> ToggleParticipation([FromBody] ToggleParticipationRequest request)
    {
        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        var userId = User.FindFirstValue("userId");
        
        // Get current player
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.IdentityUserId == userId && p.LeagueId == leagueId);
        if (player == null) return NotFound("Player not found");

        // Standardize date to UTC midnight
        var matchDate = request.Date.ToUniversalTime().Date;

        // Use a serializable transaction to prevent race conditions when joining a full match
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            Match? match = null;

            // Try to find by ID first if provided
            if (request.MatchId.HasValue)
            {
                match = await _context.Matches
                    .Include(m => m.MatchAssignments)
                    .FirstOrDefaultAsync(m => m.LeagueId == leagueId && m.Id == request.MatchId.Value);
            }

            // Fallback to date lookup if ID not provided or not found
            if (match == null)
            {
                match = await _context.Matches
                    .Include(m => m.MatchAssignments)
                    .FirstOrDefaultAsync(m => m.LeagueId == leagueId && m.Date.Date == matchDate);
            }

            if (match == null)
            {
                // Create new match if doesn't exist
                match = new Match
                {
                    Id = Guid.NewGuid(),
                    LeagueId = leagueId,
                    Date = matchDate,
                    FormatType = request.FormatType ?? "5v5",
                    IsCompleted = false
                };
                _context.Matches.Add(match);
                // Save immediately to establish the match ID and default state
                await _context.SaveChangesAsync();
            }

            // Toggle participation
            var existingAssignment = match.MatchAssignments
                .FirstOrDefault(ma => ma.PlayerId == player.Id);

            if (request.IsParticipating)
            {
                // Add player if not already in
                if (existingAssignment == null)
                {
                    // CAPACITY CHECK
                    int maxPlayers = GetMaxPlayers(match.FormatType);
                    if (match.MatchAssignments.Count >= maxPlayers)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { Message = $"Match is full (Max {maxPlayers} players)" });
                    }

                    match.MatchAssignments.Add(new MatchAssignment
                    {
                        MatchId = match.Id,
                        PlayerId = player.Id,
                        TeamNumber = 0 // Not assigned to team yet
                    });
                }
            }
            else
            {
                // Remove player
                if (existingAssignment != null)
                {
                    match.MatchAssignments.Remove(existingAssignment);
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { 
                success = true, 
                isParticipating = request.IsParticipating,
                message = request.IsParticipating ? "You're in!" : "You're out" 
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private int GetMaxPlayers(string formatType)
    {
        if (string.IsNullOrWhiteSpace(formatType)) return 16; // Default to 8v8 size

        // Try to parse "5v5", "7v7", "5-a-side", etc.
        // Simple logic: extract first number and double it
        var numberPart = new string(formatType.TakeWhile(char.IsDigit).ToArray());
        if (int.TryParse(numberPart, out int teamSize))
        {
            return teamSize * 2;
        }

        // Handle "XvX" format if it starts with non-digit (unlikely but safe)
        var parts = formatType.Split(new[] { 'v', 'V' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && int.TryParse(parts[0], out int size))
        {
            return size * 2;
        }

        return 16;
    }
}

public record CreateMatchRequest(DateTime Date, string FormatType, string? Location, List<AssignmentDto> Assignments);
public record AssignmentDto(Guid PlayerId, int TeamNumber);

public record TeamCalculationRequest(List<Guid> PlayerIds, int TeamCount);

public record ToggleParticipationRequest(DateTime Date, bool IsParticipating, Guid? MatchId = null, string? FormatType = null);

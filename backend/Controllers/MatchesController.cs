using FairPlay.Api.Data;
using FairPlay.Api.Models;
using FairPlay.Api.Services;
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
    private readonly IRatingUpdateService _updateService;

    public MatchesController(FairPlayDbContext context, ITeamBalancerService balancer, IRatingUpdateService updateService)
    {
        _context = context;
        _balancer = balancer;
        _updateService = updateService;
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
        try
        {
            await _updateService.CompleteMatchAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
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
        
        var match = new Match
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            Date = request.Date,
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

        var performance = await _context.RawRatings
            .Where(r => r.SubjectId == player.Id && recentMatches.Contains(r.MatchId))
            .GroupBy(r => r.MatchId)
            .Select(g => new { 
                Date = _context.Matches.Where(m => m.Id == g.Key).Select(m => m.Date).FirstOrDefault(),
                Value = Math.Round(g.Average(r => r.Value), 1)
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
            var hasRated = await _context.RawRatings.AnyAsync(r => r.MatchId == match.Id && r.RaterId == player.Id);
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

    [HttpPost("{id}/ratings")]
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

        // Check if already rated
        var existingRating = await _context.RawRatings.AnyAsync(r => r.MatchId == id && r.RaterId == rater.Id);
        if (existingRating) return BadRequest("You have already rated players for this match.");

        foreach (var r in ratings)
        {
            // Verify subject is in the match
            if (!match.MatchAssignments.Any(ma => ma.PlayerId == r.SubjectId)) continue;

            _context.RawRatings.Add(new RawRating
            {
                Id = Guid.NewGuid(),
                MatchId = id,
                RaterId = rater.Id,
                SubjectId = r.SubjectId,
                Value = r.Value
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Ratings submitted successfully" });
    }

    [HttpGet("{id}/my-ratings")]
    public async Task<IActionResult> GetMyRatings(Guid id)
    {
        var userId = User.FindFirstValue("userId");
        var rater = await _context.Players.FirstOrDefaultAsync(p => p.IdentityUserId == userId);
        if (rater == null) return Unauthorized();

        var ratings = await _context.RawRatings
            .Where(r => r.MatchId == id && r.RaterId == rater.Id)
            .Select(r => new { r.SubjectId, r.Value })
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

        // Get or create match for the date
        var match = await _context.Matches
            .Include(m => m.MatchAssignments)
            .FirstOrDefaultAsync(m => m.LeagueId == leagueId && m.Date.Date == request.Date.Date);

        if (match == null)
        {
            // Create new match if doesn't exist
            match = new Match
            {
                Id = Guid.NewGuid(),
                LeagueId = leagueId,
                Date = request.Date,
                FormatType = "5v5",
                IsCompleted = false
            };
            _context.Matches.Add(match);
        }

        // Toggle participation
        var existingAssignment = match.MatchAssignments
            .FirstOrDefault(ma => ma.PlayerId == player.Id);

        if (request.IsParticipating)
        {
            // Add player if not already in
            if (existingAssignment == null)
            {
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

        return Ok(new { 
            success = true, 
            isParticipating = request.IsParticipating,
            message = request.IsParticipating ? "You're in!" : "You're out" 
        });
    }
}

public record RatingSubmissionDto(Guid SubjectId, int Value);

public record CreateMatchRequest(DateTime Date, string FormatType, string? Location, List<AssignmentDto> Assignments);
public record AssignmentDto(Guid PlayerId, int TeamNumber);

public record TeamCalculationRequest(List<Guid> PlayerIds, int TeamCount);

public record ToggleParticipationRequest(DateTime Date, bool IsParticipating);

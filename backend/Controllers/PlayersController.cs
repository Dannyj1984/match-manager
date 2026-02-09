using FairPlay.Api.Data;
using FairPlay.Api.Models;
using FairPlay.Api.Middleware;
using FairPlay.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace FairPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly FairPlayDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public PlayersController(
        FairPlayDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    [LeagueContext(required: true, restrictSuperAdmin: true)]
    public async Task<IActionResult> GetPlayers()
    {
        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        
        var players = await _context.Players
            .Where(p => p.LeagueId == leagueId)
            .OrderByDescending(p => p.CurrentRating)
            .Select(p => new {
                p.Id,
                p.FullName,
                p.CurrentRating,
                p.PreferredPosition,
                p.LastPlayed,
                p.IdentityUserId
                // Role is fetched separately below to avoid N+1 if possible or just mixed in
            })
            .ToListAsync();
            
        var userIds = players.Select(p => p.IdentityUserId).Where(u => u != null).Distinct().ToList();
        var memberships = await _context.LeagueMemberships
            .Where(lm => lm.LeagueId == leagueId && userIds.Contains(lm.UserId))
            .ToDictionaryAsync(lm => lm.UserId, lm => lm.Role);

        // --- Calculate Stats In-Memory ---
        
        // 1. Fetch all necessary data
        var assignments = await _context.MatchAssignments
            .Include(ma => ma.Match)
            .Where(ma => ma.Match.LeagueId == leagueId && ma.Match.IsCompleted)
            .Select(ma => new { ma.PlayerId, ma.MatchId, MatchDate = ma.Match.Date, ma.Created })
            .ToListAsync();

        var matches = assignments.Select(a => new { a.MatchId, a.MatchDate }).Distinct().OrderByDescending(m => m.MatchDate).ToList();
        
        var fourWeeksAgo = DateTime.UtcNow.AddDays(-28);
        var ratings = await _context.PlayerRatings
            .Include(pr => pr.Match)
            .Where(pr => pr.Match.LeagueId == leagueId && pr.Match.Date >= fourWeeksAgo)
            .Select(pr => new { pr.RatedPlayerId, pr.Rating })
            .ToListAsync();

        // 2. Process per player
        var enrichedPlayers = players.Select(p => {
            var myAssignments = assignments.Where(a => a.PlayerId == p.Id).ToList();
            
            // Games Played
            var gamesPlayed = myAssignments.Count;

            // Streak
            int streak = 0;
            var playerMatchIds = myAssignments.Select(a => a.MatchId).ToHashSet();
            foreach (var m in matches)
            {
                if (playerMatchIds.Contains(m.MatchId)) streak++;
                else break;
            }

            // Rating 4 Weeks
            var myRatings = ratings.Where(r => r.RatedPlayerId == p.Id).ToList();
            decimal? rating4Weeks = myRatings.Any() ? Math.Round(myRatings.Average(r => (decimal)r.Rating), 1) : null;

            // Early Bird
            // For each match I played in, was I the first creation date?
            int earlyBird = 0;
            foreach (var assignment in myAssignments)
            {
                // Find min created for this match
                var matchAssignments = assignments.Where(a => a.MatchId == assignment.MatchId).ToList();
                if (matchAssignments.Any())
                {
                    var firstJoin = matchAssignments.OrderBy(a => a.Created).ThenBy(a => a.PlayerId).First();
                    if (firstJoin.PlayerId == p.Id) earlyBird++;
                }
            }

            return new {
                p.Id,
                p.FullName,
                p.CurrentRating,
                p.PreferredPosition,
                p.LastPlayed,
                p.IdentityUserId,
                Role = (p.IdentityUserId != null && memberships.ContainsKey(p.IdentityUserId)) ? memberships[p.IdentityUserId] : null,
                Stats = new {
                    GamesPlayed = gamesPlayed,
                    Streak = streak,
                    Rating4Weeks = rating4Weeks,
                    EarlyBird = earlyBird
                }
            };
        });
            
        return Ok(enrichedPlayers);
    }

    [HttpGet("me")]
    [LeagueContext(required: true, restrictSuperAdmin: true)]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirstValue("userId");
        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.IdentityUserId == userId && p.LeagueId == leagueId);
            
        if (player == null) return NotFound();

        return Ok(new {
            player.Id,
            player.FullName,
            player.CurrentRating,
            player.PreferredPosition,
            Email = User.Identity?.Name
        });
    }

    [HttpPut("me")]
    [LeagueContext(required: true, restrictSuperAdmin: true)]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue("userId");
        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.IdentityUserId == userId && p.LeagueId == leagueId);
            
        if (player == null) return NotFound();

        player.FullName = request.FullName;
        player.PreferredPosition = request.PreferredPosition;

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Profile updated successfully" });
    }

    [HttpPost]
    [LeagueContext(required: true, restrictSuperAdmin: true)]
    [LeagueAdmin]
    public async Task<IActionResult> Create([FromBody] CreatePlayerRequest request)
    {
        if (await _userManager.FindByEmailAsync(request.Email) != null)
        {
            return BadRequest(new { Message = "Email already in use" });
        }

        var user = new ApplicationUser { UserName = request.Email, Email = request.Email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, request.InitialPassword);

        if (!result.Succeeded) return BadRequest(result.Errors);

        // Assign User Role
        if (!await _roleManager.RoleExistsAsync("User"))
            await _roleManager.CreateAsync(new IdentityRole("User"));
        await _userManager.AddToRoleAsync(user, "User");

        // Create player record
        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        
        var player = new Player
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            FullName = request.FullName,
            CurrentRating = request.InitialRating,
            PreferredPosition = request.PreferredPosition,
            IdentityUserId = user.Id
        };
        _context.Players.Add(player);

        // Add to league as Member
        var membership = new LeagueMembership
        {
            Id = Guid.NewGuid(),
            LeagueId = leagueId,
            UserId = user.Id,
            Role = "Member",
            JoinedDate = DateTime.UtcNow
        };
        _context.LeagueMemberships.Add(membership);

        await _context.SaveChangesAsync();

        return Ok(new { 
            Id = player.Id, 
            player.FullName, 
            player.CurrentRating,
            player.PreferredPosition,
            Message = "Player created and added to league successfully" 
        });
    }

    [HttpPost("{id}/promote")]
    [LeagueContext(required: true, restrictSuperAdmin: true)]
    [LeagueAdmin]
    public async Task<IActionResult> Promote(Guid id)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
        if (player == null) return NotFound(new { Message = "Player not found" });

        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        if (player.LeagueId != leagueId) return BadRequest(new { Message = "Player is not in this league" });

        if (string.IsNullOrEmpty(player.IdentityUserId))
            return BadRequest(new { Message = "Player has no associated user account" });

        // Update League Membership Role
        var membership = await _context.LeagueMemberships
            .FirstOrDefaultAsync(lm => lm.LeagueId == leagueId && lm.UserId == player.IdentityUserId);
            
        if (membership == null) return NotFound(new { Message = "League membership not found" });

        membership.Role = "Admin";
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"{player.FullName} promoted to League Admin successfully" });
    }

    [HttpPost("{id}/demote")]
    [LeagueContext(required: true, restrictSuperAdmin: true)]
    [LeagueAdmin]
    public async Task<IActionResult> Demote(Guid id)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
        if (player == null) return NotFound(new { Message = "Player not found" });

        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        if (player.LeagueId != leagueId) return BadRequest(new { Message = "Player is not in this league" });

        if (string.IsNullOrEmpty(player.IdentityUserId))
            return BadRequest(new { Message = "Player has no associated user account" });

        // Update League Membership Role
        var membership = await _context.LeagueMemberships
            .FirstOrDefaultAsync(lm => lm.LeagueId == leagueId && lm.UserId == player.IdentityUserId);
            
        if (membership == null) return NotFound(new { Message = "League membership not found" });

        membership.Role = "Member";
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"{player.FullName} demoted to League Member successfully" });
    }

    [HttpPatch("{id}/rating")]
    [LeagueContext(restrictSuperAdmin: true), LeagueAdmin]
    public async Task<IActionResult> UpdateRating(Guid id, [FromBody] UpdateRatingRequest request)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
        if (player == null) return NotFound(new { Message = "Player not found" });

        player.CurrentRating = request.NewRating;
        await _context.SaveChangesAsync();

        return Ok(new { 
            Id = player.Id, 
            player.FullName, 
            player.CurrentRating,
            Message = "Rating updated successfully" 
        });
    }
    [HttpGet("{id}/stats")]
    [LeagueContext(required: true, restrictSuperAdmin: true)]
    [Authorize]
    public async Task<IActionResult> GetStats(Guid id)
    {
        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        
        // 1. Get Player
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.Id == id && p.LeagueId == leagueId);
        
        if (player == null) return NotFound("Player not found in this league");

        // 2. Games Played
        var gamesPlayed = await _context.MatchAssignments
            .CountAsync(ma => ma.PlayerId == player.Id && ma.Match.IsCompleted && ma.Match.LeagueId == leagueId);

        // 3. Current Streak
        // Get all completed matches for the league, ordered by date descending
        var leagueMatches = await _context.Matches
            .Where(m => m.LeagueId == leagueId && m.IsCompleted)
            .OrderByDescending(m => m.Date)
            .Select(m => new { m.Id, m.Date })
            .ToListAsync();

        var playerMatches = await _context.MatchAssignments
            .Where(ma => ma.PlayerId == player.Id && ma.Match.IsCompleted && ma.Match.LeagueId == leagueId)
            .Select(ma => ma.MatchId)
            .ToListAsync();
        
        var playerMatchIds = new HashSet<Guid>(playerMatches);

        int currentStreak = 0;
        foreach (var match in leagueMatches)
        {
            if (playerMatchIds.Contains(match.Id))
                currentStreak++;
            else
                break;
        }

        // 4. Golden Boot (Highest Average Rating in last 4 weeks)
        var fourWeeksAgo = DateTime.UtcNow.AddDays(-28);
        
        // Get all ratings in last 4 weeks for this league
        var ratings = await _context.PlayerRatings
            .Include(pr => pr.Match)
            .Where(pr => pr.Match.LeagueId == leagueId && pr.Match.Date >= fourWeeksAgo)
            .Select(pr => new { pr.RatedPlayerId, pr.Rating })
            .ToListAsync();

        // Calculate average for each player
        var playerAverages = ratings
            .GroupBy(r => r.RatedPlayerId)
            .Select(g => new { PlayerId = g.Key, Average = g.Average(r => (decimal)r.Rating) })
            .OrderByDescending(x => x.Average)
            .ToList();
        
        var myAverage = playerAverages.FirstOrDefault(p => p.PlayerId == player.Id)?.Average;
        
        // Check if I am the top (or tied for top)
        bool isGoldenBoot = false;
        if (playerAverages.Any())
        {
            var topAverage = playerAverages.First().Average;
            if (myAverage.HasValue && myAverage.Value == topAverage)
            {
                isGoldenBoot = true;
            }
        }

        // 5. Early Bird (First to join match)
        // Only count matches where I participated
        var myMatchIds = await _context.MatchAssignments
            .Where(ma => ma.PlayerId == player.Id && ma.Match.LeagueId == leagueId)
            .Select(ma => ma.MatchId)
            .ToListAsync();

        // For these matches, find who joined first
        // Note: For older matches where Created is identical/default, this will be arbitrary or stable sort based on DB.
        // Since we only care about "Going forward" mostly, this is fine.
        var earlyBirdCount = 0;

        if (myMatchIds.Any())
        {
            var firstJoiners = await _context.MatchAssignments
                .Where(ma => myMatchIds.Contains(ma.MatchId))
                .GroupBy(ma => ma.MatchId)
                .Select(g => new { 
                    MatchId = g.Key, 
                    FirstJoinerId = g.OrderBy(x => x.Created).ThenBy(x => x.PlayerId).Select(x => x.PlayerId).FirstOrDefault() 
                })
                .ToListAsync();
            
            earlyBirdCount = firstJoiners.Count(x => x.FirstJoinerId == player.Id);
        }

        return Ok(new FairPlay.Api.DTOs.PlayerStatsDto
        {
            GamesPlayed = gamesPlayed,
            CurrentStreak = currentStreak,
            HighestRating4Weeks = myAverage,
            IsGoldenBoot = isGoldenBoot,
            EarlyBirdCount = earlyBirdCount
        });
    }

    [HttpDelete("{id}")]
    [LeagueContext(required: true, restrictSuperAdmin: true)]
    [LeagueAdmin]
    public async Task<IActionResult> Delete(Guid id)
    {
         var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
        if (player == null) return NotFound(new { Message = "Player not found" });

        var leagueId = (Guid)HttpContext.Items["LeagueId"]!;
        if (player.LeagueId != leagueId) return BadRequest(new { Message = "Player is not in this league" });

         // Remove League Membership if tied to user
         if (!string.IsNullOrEmpty(player.IdentityUserId))
         {
             var membership = await _context.LeagueMemberships
                .FirstOrDefaultAsync(lm => lm.LeagueId == leagueId && lm.UserId == player.IdentityUserId);
            
            if (membership != null)
                _context.LeagueMemberships.Remove(membership);
         }

        _context.Players.Remove(player);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Player deleted successfully" });
    }
}

public record CreatePlayerRequest(string Email, string FullName, string InitialPassword, List<string> PreferredPosition, decimal InitialRating = 5.0m);
public record UpdateProfileRequest(string FullName, List<string> PreferredPosition);
public record UpdateRatingRequest(decimal NewRating);

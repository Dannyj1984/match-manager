using FairPlay.Api.Data;
using FairPlay.Api.Models;
using FairPlay.Api.Middleware;
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
                p.LastPlayed
            })
            .ToListAsync();
            
        return Ok(players);
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
}

public record CreatePlayerRequest(string Email, string FullName, string InitialPassword, string PreferredPosition, decimal InitialRating = 5.0m);
public record UpdateProfileRequest(string FullName, string PreferredPosition);
public record UpdateRatingRequest(decimal NewRating);

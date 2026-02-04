using FairPlay.Api.Data;
using FairPlay.Api.Models;
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
    public async Task<IActionResult> GetPlayers()
    {
        var players = await _context.Players
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
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirstValue("userId");
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.IdentityUserId == userId);
            
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
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue("userId");
        var player = await _context.Players
            .FirstOrDefaultAsync(p => p.IdentityUserId == userId);
            
        if (player == null) return NotFound();

        player.FullName = request.FullName;
        player.PreferredPosition = request.PreferredPosition;

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Profile updated successfully" });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreatePlayerRequest request)
    {
        if (await _userManager.FindByEmailAsync(request.Email) != null)
        {
            return BadRequest(new { Message = "Email already in use" });
        }

        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await _userManager.CreateAsync(user, request.InitialPassword);

        if (!result.Succeeded) return BadRequest(result.Errors);

        // Assign User Role
        if (!await _roleManager.RoleExistsAsync("User"))
            await _roleManager.CreateAsync(new IdentityRole("User"));
        await _userManager.AddToRoleAsync(user, "User");

        // Create player record
        var player = new Player
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            CurrentRating = request.InitialRating,
            PreferredPosition = request.PreferredPosition,
            IdentityUserId = user.Id
        };
        _context.Players.Add(player);
        
        user.PlayerId = player.Id;
        await _context.SaveChangesAsync();

        return Ok(new { 
            Id = player.Id, 
            player.FullName, 
            player.CurrentRating,
            player.PreferredPosition,
            Message = "Player and user created successfully" 
        });
    }

    [HttpPost("{id}/promote")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Promote(Guid id)
    {
        var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
        if (player == null) return NotFound(new { Message = "Player not found" });

        if (string.IsNullOrEmpty(player.IdentityUserId))
            return BadRequest(new { Message = "Player has no associated user account" });

        var user = await _userManager.FindByIdAsync(player.IdentityUserId);
        if (user == null) return NotFound(new { Message = "User not found" });

        // Ensure Admin role exists
        if (!await _roleManager.RoleExistsAsync("Admin"))
            await _roleManager.CreateAsync(new IdentityRole("Admin"));

        // Add to role if not already in it
        if (!await _userManager.IsInRoleAsync(user, "Admin"))
        {
            var result = await _userManager.AddToRoleAsync(user, "Admin");
            if (!result.Succeeded) return BadRequest(result.Errors);
        }

        return Ok(new { Message = $"{player.FullName} promoted to Admin successfully" });
    }

    [HttpPatch("{id}/rating")]
    [Authorize(Roles = "Admin")]
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

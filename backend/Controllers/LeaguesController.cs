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
[Authorize]
public class LeaguesController : ControllerBase
{
    private readonly FairPlayDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeaguesController(FairPlayDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET /api/leagues - Get all leagues user is a member of
    [HttpGet]
    public async Task<IActionResult> GetUserLeagues()
    {
        var userId = User.FindFirstValue("userId");
        var user = await _userManager.FindByIdAsync(userId!);
        
        if (user == null) return Unauthorized();

        // Super admin can see all leagues
        if (user.IsSuperAdmin)
        {
            var allLeagues = await _context.Leagues
                .Where(l => l.IsActive)
                .Select(l => new
                {
                    l.Id,
                    l.Name,
                    l.Sport,
                    l.MaxTeams,
                    l.Location,
                    l.Description,
                    Role = "SuperAdmin"
                })
                .ToListAsync();
            return Ok(allLeagues);
        }

        // Regular users see only their leagues
        var leagues = await _context.LeagueMemberships
            .Where(lm => lm.UserId == userId)
            .Include(lm => lm.League)
            .Where(lm => lm.League!.IsActive)
            .Select(lm => new
            {
                lm.League!.Id,
                lm.League.Name,
                lm.League.Sport,
                lm.League.MaxTeams,
                lm.League.Location,
                lm.League.Description,
                lm.League.Cost,
                lm.Role
            })
            .ToListAsync();

        return Ok(leagues);
    }

    // GET /api/leagues/{id} - Get specific league details
    [HttpGet("{id}")]
    [LeagueContext]
    public async Task<IActionResult> GetLeague(Guid id)
    {
        var league = await _context.Leagues
            .Where(l => l.Id == id && l.IsActive)
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.Sport,
                l.MaxTeams,
                l.Location,
                l.Description,
                l.Cost,
                l.CreatedDate
            })
            .FirstOrDefaultAsync();

        if (league == null) return NotFound();
        return Ok(league);
    }

    // POST /api/leagues - Create new league (Super Admin only)
    [HttpPost]
    [SuperAdmin]
    public async Task<IActionResult> CreateLeague([FromBody] CreateLeagueRequest request)
    {
        var userId = User.FindFirstValue("userId");
        
        var league = new League
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Sport = request.Sport,
            MaxTeams = request.MaxTeams,
            Location = request.Location,
            Description = request.Description,
            Cost = request.Cost,
            CreatedByUserId = userId!,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };

        _context.Leagues.Add(league);

        // Add initial admin if specified
        if (!string.IsNullOrEmpty(request.InitialAdminUserId))
        {
            var membership = new LeagueMembership
            {
                Id = Guid.NewGuid(),
                LeagueId = league.Id,
                UserId = request.InitialAdminUserId,
                Role = "Admin",
                JoinedDate = DateTime.UtcNow
            };
            _context.LeagueMemberships.Add(membership);
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLeague), new { id = league.Id }, new
        {
            league.Id,
            league.Name,
            league.Sport,
            league.MaxTeams,
            league.Location,
            Message = "League created successfully"
        });
    }

    // PUT /api/leagues/{id} - Update league (League admins only)
    [HttpPut("{id}")]
    [LeagueContext, LeagueAdmin]
    public async Task<IActionResult> UpdateLeague(Guid id, [FromBody] UpdateLeagueRequest request)
    {
        var league = await _context.Leagues.FindAsync(id);
        if (league == null || !league.IsActive) return NotFound();

        league.Name = request.Name;
        league.Sport = request.Sport;
        league.MaxTeams = request.MaxTeams;
        league.Location = request.Location;
        league.Description = request.Description;
        league.Cost = request.Cost;

        await _context.SaveChangesAsync();

        return Ok(new { Message = "League updated successfully" });
    }

    // DELETE /api/leagues/{id} - Soft delete league (Super Admin only)
    [HttpDelete("{id}")]
    [SuperAdmin]
    public async Task<IActionResult> DeleteLeague(Guid id)
    {
        var league = await _context.Leagues.FindAsync(id);
        if (league == null) return NotFound();

        league.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(new { Message = "League deleted successfully" });
    }

    // GET /api/leagues/{id}/members - List league members (League admins only)
    [HttpGet("{id}/members")]
    [LeagueContext, LeagueAdmin]
    public async Task<IActionResult> GetLeagueMembers(Guid id)
    {
        var members = await _context.LeagueMemberships
            .Where(lm => lm.LeagueId == id)
            .Include(lm => lm.User)
            .Select(lm => new
            {
                UserId = lm.UserId,
                Email = lm.User!.Email,
                lm.Role,
                lm.JoinedDate
            })
            .ToListAsync();

        return Ok(members);
    }

    // POST /api/leagues/{id}/members - Add member to league (League admins only)
    [HttpPost("{id}/members")]
    [LeagueContext, LeagueAdmin]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) return NotFound(new { Message = "User not found" });

        // Check if already a member
        var existing = await _context.LeagueMemberships
            .FirstOrDefaultAsync(lm => lm.LeagueId == id && lm.UserId == user.Id);

        if (existing != null)
            return BadRequest(new { Message = "User is already a member of this league" });

        var membership = new LeagueMembership
        {
            Id = Guid.NewGuid(),
            LeagueId = id,
            UserId = user.Id,
            Role = "Member",
            JoinedDate = DateTime.UtcNow
        };

        _context.LeagueMemberships.Add(membership);
        
        // Check if player record already exists
        var existingPlayer = await _context.Players
            .FirstOrDefaultAsync(p => p.IdentityUserId == user.Id && p.LeagueId == id);
        
        Guid playerId;
        
        if (existingPlayer == null)
        {
            // Create a Player record so they can participate in matches
            var player = new Player
            {
                Id = Guid.NewGuid(),
                FullName = user.Email ?? "Player",
                IdentityUserId = user.Id,
                LeagueId = id,
                CurrentRating = 5,
                PreferredPosition = "Any"
            };
            
            _context.Players.Add(player);
            playerId = player.Id;
        }
        else
        {
            playerId = existingPlayer.Id;
        }
        
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Member added successfully", PlayerId = playerId });
    }

    // DELETE /api/leagues/{id}/members/{userId} - Remove member (League admins only)
    [HttpDelete("{id}/members/{userId}")]
    [LeagueContext, LeagueAdmin]
    public async Task<IActionResult> RemoveMember(Guid id, string userId)
    {
        var membership = await _context.LeagueMemberships
            .FirstOrDefaultAsync(lm => lm.LeagueId == id && lm.UserId == userId);

        if (membership == null) return NotFound(new { Message = "Member not found" });

        _context.LeagueMemberships.Remove(membership);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Member removed successfully" });
    }

    // POST /api/leagues/{id}/admins/{userId} - Promote user to league admin
    [HttpPost("{id}/admins/{userId}")]
    [LeagueContext, LeagueAdmin]
    public async Task<IActionResult> PromoteToAdmin(Guid id, string userId)
    {
        var membership = await _context.LeagueMemberships
            .FirstOrDefaultAsync(lm => lm.LeagueId == id && lm.UserId == userId);

        if (membership == null) return NotFound(new { Message = "Member not found" });

        membership.Role = "Admin";
        await _context.SaveChangesAsync();

        return Ok(new { Message = "User promoted to admin successfully" });
    }

    // DELETE /api/leagues/{id}/admins/{userId} - Demote admin to member
    [HttpDelete("{id}/admins/{userId}")]
    [LeagueContext, LeagueAdmin]
    public async Task<IActionResult> DemoteAdmin(Guid id, string userId)
    {
        var membership = await _context.LeagueMemberships
            .FirstOrDefaultAsync(lm => lm.LeagueId == id && lm.UserId == userId);

        if (membership == null) return NotFound(new { Message = "Member not found" });

        membership.Role = "Member";
        await _context.SaveChangesAsync();

        return Ok(new { Message = "User demoted to member successfully" });
    }

    // POST /api/leagues/{id}/create-admin - Create new league admin account
    [HttpPost("{id}/create-admin")]
    [LeagueContext]
    public async Task<IActionResult> CreateLeagueAdmin(Guid id, [FromBody] CreateLeagueAdminRequest request)
    {
        var userId = User.FindFirstValue("userId");
        var currentUser = await _userManager.FindByIdAsync(userId!);
        
        if (currentUser == null) return Unauthorized();
        
        // Only super admins or existing league admins can create league admins
        var isSuperAdmin = currentUser.IsSuperAdmin;
        var isLeagueAdmin = false;
        
        if (!isSuperAdmin)
        {
            var adminMembership = await _context.LeagueMemberships
                .FirstOrDefaultAsync(lm => lm.LeagueId == id && lm.UserId == userId && lm.Role == "Admin");
            isLeagueAdmin = adminMembership != null;
        }
        
        if (!isSuperAdmin && !isLeagueAdmin)
        {
            return Forbid();
        }
        
        // Check if user already exists
        if (await _userManager.FindByEmailAsync(request.Email) != null)
        {
            return BadRequest(new { Message = "Email already in use" });
        }
        
        // Verify league exists
        var league = await _context.Leagues.FindAsync(id);
        if (league == null || !league.IsActive)
        {
            return NotFound(new { Message = "League not found" });
        }
        
        // Create user account
        var newUser = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };
        
        var createResult = await _userManager.CreateAsync(newUser, request.Password);
        if (!createResult.Succeeded)
        {
            return BadRequest(new { Message = "Failed to create user", Errors = createResult.Errors });
        }
        
        // Create player record
        var player = new Player
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            IdentityUserId = newUser.Id,
            LeagueId = id,
            CurrentRating = 5,
            PreferredPosition = request.PreferredPosition ?? "Any"
        };
        
        _context.Players.Add(player);
        
        // Create league membership with Admin role
        var membership = new LeagueMembership
        {
            Id = Guid.NewGuid(),
            LeagueId = id,
            UserId = newUser.Id,
            Role = "Admin",
            JoinedDate = DateTime.UtcNow
        };
        
        _context.LeagueMemberships.Add(membership);
        
        await _context.SaveChangesAsync();
        
        return Ok(new
        {
            Message = "League admin created successfully",
            UserId = newUser.Id,
            PlayerId = player.Id,
            Email = newUser.Email,
            FullName = player.FullName
        });
    }
}

// DTOs
public record LeagueDto(Guid Id, string Name, string Sport, int MaxTeams, string? Location, string? Description, decimal Cost, string Role);
public record CreateLeagueRequest(string Name, string Sport, int MaxTeams, string? Location, string? Description, decimal Cost, string? InitialAdminUserId);
public record UpdateLeagueRequest(string Name, string Sport, int MaxTeams, string? Location, string? Description, decimal Cost);
public record AddMemberRequest(string Email);
public record CreateLeagueAdminRequest(string Email, string FullName, string Password, string? PreferredPosition);

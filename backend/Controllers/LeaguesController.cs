using FairPlay.Api.Data;
using FairPlay.Api.Models;
using FairPlay.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Net.Http.Json;

namespace FairPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaguesController : ControllerBase
{
    private readonly FairPlayDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpClientFactory _httpClientFactory;

    public LeaguesController(FairPlayDbContext context, UserManager<ApplicationUser> userManager, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _userManager = userManager;
        _httpClientFactory = httpClientFactory;
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
                    l.AllowRatings,
                    l.IsPublic,
                    l.Postcode,
                    Role = "SuperAdmin",
                    PendingJoinRequests = _context.LeagueJoinRequests
                        .Count(jr => jr.LeagueId == l.Id && jr.Status == "Pending")
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
                lm.League.AllowRatings,
                lm.League.IsPublic,
                lm.League.Postcode,
                lm.Role,
                PendingJoinRequests = lm.Role == "Admin"
                    ? _context.LeagueJoinRequests.Count(jr => jr.LeagueId == lm.LeagueId && jr.Status == "Pending")
                    : 0
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
                l.AllowRatings,
                l.IsPublic,
                l.Postcode,
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
            AllowRatings = request.AllowRatings,
            IsPublic = request.IsPublic,
            Postcode = request.Postcode,
            CreatedByUserId = userId!,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };

        // Geocode postcode if provided
        if (!string.IsNullOrWhiteSpace(request.Postcode))
        {
            var coords = await GeocodePostcodeAsync(request.Postcode);
            if (coords.HasValue)
            {
                league.Latitude = coords.Value.lat;
                league.Longitude = coords.Value.lng;
            }
        }

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
            league.AllowRatings,
            league.IsPublic,
            league.Postcode,
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

        var postcodeChanged = !string.Equals(league.Postcode, request.Postcode, StringComparison.OrdinalIgnoreCase);

        league.Name = request.Name;
        league.Sport = request.Sport;
        league.MaxTeams = request.MaxTeams;
        league.Location = request.Location;
        league.Description = request.Description;
        league.Cost = request.Cost;
        league.AllowRatings = request.AllowRatings;
        league.IsPublic = request.IsPublic;
        league.Postcode = request.Postcode;

        // Re-geocode if postcode changed
        if (postcodeChanged)
        {
            if (!string.IsNullOrWhiteSpace(request.Postcode))
            {
                var coords = await GeocodePostcodeAsync(request.Postcode);
                if (coords.HasValue)
                {
                    league.Latitude = coords.Value.lat;
                    league.Longitude = coords.Value.lng;
                }
                else
                {
                    league.Latitude = null;
                    league.Longitude = null;
                }
            }
            else
            {
                league.Latitude = null;
                league.Longitude = null;
            }
        }

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

    // GET /api/leagues/search - Search public leagues by postcode + radius
    [HttpGet("search")]
    public async Task<IActionResult> SearchPublicLeagues([FromQuery] string postcode, [FromQuery] int radiusMiles = 5, [FromQuery] string? sport = null)
    {
        if (string.IsNullOrWhiteSpace(postcode))
            return BadRequest(new { Message = "Postcode is required" });

        // Geocode search postcode
        var searchCoords = await GeocodePostcodeAsync(postcode);
        if (!searchCoords.HasValue)
            return BadRequest(new { Message = "Could not find the specified postcode" });

        var userId = User.FindFirstValue("userId");

        // Get all public, active leagues that have coordinates
        var query = _context.Leagues
            .Where(l => l.IsActive && l.IsPublic && l.Latitude != null && l.Longitude != null);

        // Filter by sport if provided
        if (!string.IsNullOrEmpty(sport) && !sport.Equals("Any", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(l => l.Sport == sport);
        }

        var publicLeagues = await query
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.Sport,
                l.MaxTeams,
                l.Location,
                l.Description,
                l.Cost,
                l.Postcode,
                l.Latitude,
                l.Longitude,
                MemberCount = _context.LeagueMemberships.Count(lm => lm.LeagueId == l.Id),
                IsAlreadyMember = _context.LeagueMemberships.Any(lm => lm.LeagueId == l.Id && lm.UserId == userId),
                HasPendingRequest = _context.LeagueJoinRequests.Any(jr => jr.LeagueId == l.Id && jr.UserId == userId && jr.Status == "Pending")
            })
            .ToListAsync();

        // Filter by distance in-memory using Haversine
        var distanceResults = publicLeagues
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.Sport,
                l.MaxTeams,
                l.Location,
                l.Description,
                l.Cost,
                l.Postcode,
                l.MemberCount,
                l.IsAlreadyMember,
                l.HasPendingRequest,
                DistanceMiles = CalculateDistanceMiles(
                    searchCoords.Value.lat, searchCoords.Value.lng,
                    l.Latitude!.Value, l.Longitude!.Value)
            })
            .OrderBy(l => l.DistanceMiles)
            .ToList();
            
        var results = distanceResults
            .Where(l => l.DistanceMiles <= radiusMiles)
            .ToList();

        // If no results found within radius, return the nearest one (if any exists)
        if (results.Count == 0 && distanceResults.Count > 0)
        {
            results.Add(distanceResults.First());
        }

        return Ok(results);

        return Ok(results);
    }

    // GET /api/leagues/{id}/public - Get public league details (for non-members)
    [HttpGet("{id}/public")]
    public async Task<IActionResult> GetPublicLeague(Guid id)
    {
        var userId = User.FindFirstValue("userId");

        var league = await _context.Leagues
            .Where(l => l.Id == id && l.IsActive && l.IsPublic)
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.Sport,
                l.MaxTeams,
                l.Location,
                l.Description,
                l.Cost,
                l.Postcode,
                l.CreatedDate,
                MemberCount = _context.LeagueMemberships.Count(lm => lm.LeagueId == l.Id),
                IsAlreadyMember = _context.LeagueMemberships.Any(lm => lm.LeagueId == l.Id && lm.UserId == userId),
                HasPendingRequest = _context.LeagueJoinRequests.Any(jr => jr.LeagueId == l.Id && jr.UserId == userId && jr.Status == "Pending")
            })
            .FirstOrDefaultAsync();

        if (league == null) return NotFound();
        return Ok(league);
    }

    // POST /api/leagues/{id}/join - Request to join a league
    [HttpPost("{id}/join")]
    public async Task<IActionResult> RequestToJoin(Guid id)
    {
        var userId = User.FindFirstValue("userId");

        var league = await _context.Leagues.FindAsync(id);
        if (league == null || !league.IsActive || !league.IsPublic)
            return NotFound(new { Message = "League not found" });

        // Check if already a member
        var existingMembership = await _context.LeagueMemberships
            .FirstOrDefaultAsync(lm => lm.LeagueId == id && lm.UserId == userId);
        if (existingMembership != null)
            return BadRequest(new { Message = "You are already a member of this league" });

        // Check for existing pending request
        var existingRequest = await _context.LeagueJoinRequests
            .FirstOrDefaultAsync(jr => jr.LeagueId == id && jr.UserId == userId && jr.Status == "Pending");
        if (existingRequest != null)
            return BadRequest(new { Message = "You already have a pending request for this league" });

        var joinRequest = new LeagueJoinRequest
        {
            Id = Guid.NewGuid(),
            LeagueId = id,
            UserId = userId!,
            Status = "Pending",
            RequestedDate = DateTime.UtcNow
        };

        _context.LeagueJoinRequests.Add(joinRequest);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Join request submitted successfully" });
    }

    // GET /api/leagues/{id}/join-requests - List pending join requests (League admins)
    [HttpGet("{id}/join-requests")]
    [LeagueContext, LeagueAdmin]
    public async Task<IActionResult> GetJoinRequests(Guid id)
    {
        var requests = await _context.LeagueJoinRequests
            .Where(jr => jr.LeagueId == id && jr.Status == "Pending")
            .Include(jr => jr.User)
            .Select(jr => new
            {
                jr.Id,
                jr.UserId,
                Email = jr.User!.Email,
                jr.RequestedDate,
                FullName = _context.Players
                    .Where(p => p.IdentityUserId == jr.UserId)
                    .Select(p => p.FullName)
                    .FirstOrDefault() ?? "Unknown"
            })
            .OrderBy(jr => jr.RequestedDate)
            .ToListAsync();

        return Ok(requests);
    }

    // POST /api/leagues/{id}/join-requests/{requestId}/approve - Approve join request
    [HttpPost("{id}/join-requests/{requestId}/approve")]
    [LeagueContext, LeagueAdmin]
    public async Task<IActionResult> ApproveJoinRequest(Guid id, Guid requestId)
    {
        var joinRequest = await _context.LeagueJoinRequests
            .FirstOrDefaultAsync(jr => jr.Id == requestId && jr.LeagueId == id && jr.Status == "Pending");

        if (joinRequest == null)
            return NotFound(new { Message = "Join request not found" });

        var adminUserId = User.FindFirstValue("userId");

        // Update request status
        joinRequest.Status = "Approved";
        joinRequest.ReviewedDate = DateTime.UtcNow;
        joinRequest.ReviewedByUserId = adminUserId;

        // Check if already a member (safety check)
        var existingMembership = await _context.LeagueMemberships
            .FirstOrDefaultAsync(lm => lm.LeagueId == id && lm.UserId == joinRequest.UserId);

        if (existingMembership == null)
        {
            // Create membership
            var membership = new LeagueMembership
            {
                Id = Guid.NewGuid(),
                LeagueId = id,
                UserId = joinRequest.UserId,
                Role = "Member",
                JoinedDate = DateTime.UtcNow
            };
            _context.LeagueMemberships.Add(membership);

            // Create player record if one doesn't exist
            var existingPlayer = await _context.Players
                .FirstOrDefaultAsync(p => p.IdentityUserId == joinRequest.UserId && p.LeagueId == id);

            if (existingPlayer == null)
            {
                var user = await _userManager.FindByIdAsync(joinRequest.UserId);
                var player = new Player
                {
                    Id = Guid.NewGuid(),
                    FullName = user?.Email ?? "Player",
                    IdentityUserId = joinRequest.UserId,
                    LeagueId = id,
                    CurrentRating = 5,
                    PreferredPosition = new List<string> { "Any" }
                };
                _context.Players.Add(player);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Join request approved successfully" });
    }

    // POST /api/leagues/{id}/join-requests/{requestId}/reject - Reject join request
    [HttpPost("{id}/join-requests/{requestId}/reject")]
    [LeagueContext, LeagueAdmin]
    public async Task<IActionResult> RejectJoinRequest(Guid id, Guid requestId)
    {
        var joinRequest = await _context.LeagueJoinRequests
            .FirstOrDefaultAsync(jr => jr.Id == requestId && jr.LeagueId == id && jr.Status == "Pending");

        if (joinRequest == null)
            return NotFound(new { Message = "Join request not found" });

        var adminUserId = User.FindFirstValue("userId");

        joinRequest.Status = "Rejected";
        joinRequest.ReviewedDate = DateTime.UtcNow;
        joinRequest.ReviewedByUserId = adminUserId;

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Join request rejected" });
    }

    // GET /api/leagues/my-requests - Get current user's pending join requests
    [HttpGet("my-requests")]
    public async Task<IActionResult> GetMyJoinRequests()
    {
        var userId = User.FindFirstValue("userId");

        var requests = await _context.LeagueJoinRequests
            .Where(jr => jr.UserId == userId && jr.Status == "Pending")
            .Include(jr => jr.League)
            .Select(jr => new
            {
                jr.Id,
                jr.LeagueId,
                LeagueName = jr.League!.Name,
                LeagueSport = jr.League.Sport,
                jr.RequestedDate
            })
            .ToListAsync();

        return Ok(requests);
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
                PreferredPosition = new List<string> { "Any" }
            };
            
            _context.Players.Add(player);
            playerId = player.Id;
        }
        else
        {
            playerId = existingPlayer.Id;
        }
        
        // Also approve any pending join request from this user
        var pendingRequest = await _context.LeagueJoinRequests
            .FirstOrDefaultAsync(jr => jr.LeagueId == id && jr.UserId == user.Id && jr.Status == "Pending");
        if (pendingRequest != null)
        {
            pendingRequest.Status = "Approved";
            pendingRequest.ReviewedDate = DateTime.UtcNow;
            pendingRequest.ReviewedByUserId = User.FindFirstValue("userId");
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
            PreferredPosition = request.PreferredPosition ?? new List<string> { "Any" }
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

    // --- Helper Methods ---

    private async Task<(double lat, double lng)?> GeocodePostcodeAsync(string postcode)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetFromJsonAsync<PostcodesIoResponse>(
                $"https://api.postcodes.io/postcodes/{Uri.EscapeDataString(postcode.Trim())}");
            
            if (response?.Status == 200 && response.Result != null)
                return (response.Result.Latitude, response.Result.Longitude);
        }
        catch (Exception)
        {
            // Geocoding failed — league will be saved without coordinates
        }
        return null;
    }

    private static double CalculateDistanceMiles(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 3958.8; // Earth's radius in miles
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(R * c, 1);
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}

// DTOs
public record LeagueDto(Guid Id, string Name, string Sport, int MaxTeams, string? Location, string? Description, decimal Cost, bool AllowRatings, bool IsPublic, string? Postcode, string Role);
public record CreateLeagueRequest(string Name, string Sport, int MaxTeams, string? Location, string? Description, decimal Cost, bool AllowRatings, bool IsPublic, string? Postcode, string? InitialAdminUserId);
public record UpdateLeagueRequest(string Name, string Sport, int MaxTeams, string? Location, string? Description, decimal Cost, bool AllowRatings, bool IsPublic, string? Postcode);
public record AddMemberRequest(string Email);
public record CreateLeagueAdminRequest(string Email, string FullName, string Password, List<string>? PreferredPosition);

// Postcodes.io response DTOs
public class PostcodesIoResponse
{
    public int Status { get; set; }
    public PostcodesIoResult? Result { get; set; }
}

public class PostcodesIoResult
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

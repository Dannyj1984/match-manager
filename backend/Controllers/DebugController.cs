using FairPlay.Api.Data;
using FairPlay.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FairPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly FairPlayDbContext _context;

    public DebugController(UserManager<ApplicationUser> userManager, FairPlayDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpGet("check-superadmin")]
    [Authorize]
    public async Task<IActionResult> CheckSuperAdmin()
    {
        var userId = User.FindFirstValue("userId");
        var user = await _userManager.FindByIdAsync(userId!);

        if (user == null)
            return NotFound("User not found");

        // Get all claims from the token
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

        // Check database
        var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        return Ok(new
        {
            UserId = userId,
            Email = user.Email,
            IsSuperAdminFromToken = User.FindFirstValue("isSuperAdmin"),
            IsSuperAdminFromDatabase = user.IsSuperAdmin,
            IsSuperAdminFromDbContext = dbUser?.IsSuperAdmin,
            IsAuthenticated = User.Identity?.IsAuthenticated,
            AllClaims = claims
        });
    }

    [HttpGet("all-users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new 
            { 
                u.Id, 
                u.Email, 
                u.IsSuperAdmin,
                u.UserName
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("set-superadmin/{email}")]
    public async Task<IActionResult> SetSuperAdmin(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return NotFound("User not found");

        user.IsSuperAdmin = true;
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Set {email} as super admin", IsSuperAdmin = user.IsSuperAdmin });
    }
}

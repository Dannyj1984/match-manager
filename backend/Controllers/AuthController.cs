using FairPlay.Api.Data;
using FairPlay.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FairPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly FairPlayDbContext _context;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        FairPlayDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded) return BadRequest(result.Errors);

        // Assign Role
        if (request.IsAdmin)
        {
            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            await _userManager.AddToRoleAsync(user, "Admin");
        }
        else
        {
            if (!await _roleManager.RoleExistsAsync("User"))
                await _roleManager.CreateAsync(new IdentityRole("User"));
            await _userManager.AddToRoleAsync(user, "User");
        }

        // Create associated Player record
        var player = new Player
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            IdentityUserId = user.Id
        };
        _context.Players.Add(player);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var player = await _context.Players.FirstOrDefaultAsync(p => p.IdentityUserId == user.Id);

        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("userId", user.Id),
            new Claim("playerId", player?.Id.ToString() ?? ""),
            new Claim("isSuperAdmin", user.IsSuperAdmin.ToString())
        };

        foreach (var role in roles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "a_very_long_secret_key_for_development_only_123456"));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "FairPlayApi",
            audience: _configuration["Jwt:Issuer"] ?? "FairPlayApi",
            expires: DateTime.Now.AddHours(3),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiration = token.ValidTo,
            user = new { user.Email, roles, playerId = player?.Id, isSuperAdmin = user.IsSuperAdmin }
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue("userId");
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var player = await _context.Players.FirstOrDefaultAsync(p => p.IdentityUserId == user.Id);

        return Ok(new
        {
            user = new { user.Email, roles, playerId = player?.Id, isSuperAdmin = user.IsSuperAdmin }
        });
    }
}

public record RegisterRequest(string Email, string Password, string FullName, bool IsAdmin = false);
public record LoginRequest(string Email, string Password);

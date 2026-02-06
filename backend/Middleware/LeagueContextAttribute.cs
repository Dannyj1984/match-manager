using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FairPlay.Api.Models;
using FairPlay.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FairPlay.Api.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LeagueContextAttribute : Attribute, IAsyncAuthorizationFilter, IOrderedFilter
{
    private readonly bool _required;
    private readonly bool _restrictSuperAdmin;
    public int Order { get; set; } = -1; // Run before other auth filters (like LeagueAdmin)

    public LeagueContextAttribute(bool required = false, bool restrictSuperAdmin = false)
    {
        _required = required;
        _restrictSuperAdmin = restrictSuperAdmin;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var dbContext = context.HttpContext.RequestServices.GetRequiredService<FairPlayDbContext>();
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Get league ID from header or query parameter
        var leagueIdStr = context.HttpContext.Request.Headers["X-League-Id"].FirstOrDefault() 
                         ?? context.HttpContext.Request.Query["leagueId"].FirstOrDefault();
        
        // If no league ID provided and it's not required, just continue
        if (string.IsNullOrEmpty(leagueIdStr))
        {
            if (_required)
            {
                context.Result = new BadRequestObjectResult(new { Message = "League ID is required" });
                return;
            }
            
            context.HttpContext.Items["LeagueId"] = null;
            context.HttpContext.Items["IsSuperAdmin"] = false;
            return;
        }
        
        if (!Guid.TryParse(leagueIdStr, out var leagueId))
        {
            context.Result = new BadRequestObjectResult(new { Message = "Invalid League ID format" });
            return;
        }
        
        // Get current user
        var userId = context.HttpContext.User.FindFirstValue("userId");
        if (userId == null)
        {
             context.Result = new UnauthorizedResult();
             return;
        }

        var user = await userManager.FindByIdAsync(userId!);
        
        if (user == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }
        
        // Super admins have access to all leagues WITHOUT membership check UNLESS restricted
        if (user.IsSuperAdmin && !_restrictSuperAdmin)
        {
            context.HttpContext.Items["LeagueId"] = leagueId;
            context.HttpContext.Items["IsSuperAdmin"] = true;
            return;
        }
        
        // Check if user is a member of the league (Required for regular users OR restricted SuperAdmins)
        var membership = await dbContext.LeagueMemberships
            .FirstOrDefaultAsync(lm => lm.LeagueId == leagueId && lm.UserId == user.Id);
        
        if (membership == null)
        {
            context.Result = new StatusCodeResult(403); // Forbidden
            return;
        }
        
        // Store league context for use in controllers
        context.HttpContext.Items["LeagueId"] = leagueId;
        context.HttpContext.Items["LeagueMembershipRole"] = membership.Role;
        context.HttpContext.Items["IsSuperAdmin"] = user.IsSuperAdmin; // Still flag as super admin, but they are also a member now
    }
}

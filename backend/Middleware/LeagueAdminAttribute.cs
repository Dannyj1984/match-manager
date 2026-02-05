using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FairPlay.Api.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LeagueAdminAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Must be used in conjunction with LeagueContextAttribute
        if (!context.HttpContext.Items.ContainsKey("LeagueId"))
        {
            context.Result = new BadRequestObjectResult(new { Message = "League context is required" });
            return;
        }
        
        var isSuperAdmin = context.HttpContext.Items["IsSuperAdmin"] as bool? ?? false;
        
        // Super admins have full access
        if (isSuperAdmin)
        {
            return;
        }
        
        var role = context.HttpContext.Items["LeagueMembershipRole"] as string;
        
        // Check if user has Admin role in the league
        if (role != "Admin")
        {
            context.Result = new ForbidResult();
            return;
        }
        
        await Task.CompletedTask;
    }
}

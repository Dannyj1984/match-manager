using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FairPlay.Api.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace FairPlay.Api.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SuperAdminAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<SuperAdminAttribute>>();
        
        // Check if user is authenticated first
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            logger.LogWarning("SuperAdmin check FAILED - User not authenticated");
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get userId from claims
        var userId = context.HttpContext.User.FindFirstValue("userId");
        
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("SuperAdmin check FAILED - userId claim not found");
            context.Result = new UnauthorizedResult();
            return;
        }

        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Use FindByIdAsync directly with the userId claim
        var user = await userManager.FindByIdAsync(userId);
        
        if (user == null)
        {
            logger.LogWarning("SuperAdmin check FAILED - User {UserId} not found in database", userId);
            context.Result = new UnauthorizedResult();
            return;
        }

        logger.LogInformation("SuperAdmin check - User: {Email}, IsSuperAdmin: {IsSuperAdmin}", user.Email, user.IsSuperAdmin);

        if (!user.IsSuperAdmin)
        {
            logger.LogWarning("SuperAdmin check FAILED - User {Email} is not a super admin", user.Email);
            context.Result = new ForbidResult();
        }
        else
        {
            logger.LogInformation("SuperAdmin check PASSED for user {Email}", user.Email);
        }
    }
}

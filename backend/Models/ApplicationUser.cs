using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace FairPlay.Api.Models;

public class ApplicationUser : IdentityUser
{
    // Super Admin flag
    public bool IsSuperAdmin { get; set; } = false;
    
    // Navigation properties
    [JsonIgnore]
    public List<LeagueMembership> LeagueMemberships { get; set; } = new();
}

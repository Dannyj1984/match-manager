using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FairPlay.Api.Models;

public class LeagueMembership
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid LeagueId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "Member"; // "Admin" or "Member"
    
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [JsonIgnore]
    public League? League { get; set; }
    
    [JsonIgnore]
    public ApplicationUser? User { get; set; }
}

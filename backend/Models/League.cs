using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FairPlay.Api.Models;

public class League
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Sport { get; set; } = "Football"; // Football, Netball, Basketball, Rugby
    
    [Required]
    public int MaxTeams { get; set; } = 2;
    
    [MaxLength(200)]
    public string? Location { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public decimal Cost { get; set; } = 0.00m;
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    [Required]
    public string CreatedByUserId { get; set; } = string.Empty; // Super Admin who created this league
    
    public bool IsActive { get; set; } = true;
    
    public bool AllowRatings { get; set; } = true;
    
    public bool IsPublic { get; set; } = false;
    
    [MaxLength(10)]
    public string? Postcode { get; set; }
    
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    // Navigation properties
    [JsonIgnore]
    public ApplicationUser? CreatedBy { get; set; }
    
    [JsonIgnore]
    public List<Player> Players { get; set; } = new();
    
    [JsonIgnore]
    public List<Match> Matches { get; set; } = new();
    
    [JsonIgnore]
    public List<LeagueMembership> Memberships { get; set; } = new();
}

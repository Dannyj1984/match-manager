using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FairPlay.Api.Models;

public class LeagueJoinRequest
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid LeagueId { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    
    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? ReviewedDate { get; set; }
    
    public string? ReviewedByUserId { get; set; }
    
    // Navigation properties
    [JsonIgnore]
    public League? League { get; set; }
    
    [JsonIgnore]
    public ApplicationUser? User { get; set; }
    
    [JsonIgnore]
    public ApplicationUser? ReviewedBy { get; set; }
}

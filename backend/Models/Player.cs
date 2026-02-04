using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FairPlay.Api.Models;

public class Player
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
    
    public decimal CurrentRating { get; set; } = 5.0m; // Default starting rating
    
    [Required]
    [MaxLength(10)]
    public string PreferredPosition { get; set; } = "M"; // GK, D, M, A
    
    public DateTime? LastPlayed { get; set; }

    // Link to Identity User
    public string? IdentityUserId { get; set; }
    [JsonIgnore]
    public ApplicationUser? User { get; set; }
    
    [JsonIgnore]
    public List<MatchAssignment> MatchAssignments { get; set; } = new();
}

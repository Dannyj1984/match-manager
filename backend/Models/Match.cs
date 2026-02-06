using System.ComponentModel.DataAnnotations;

namespace FairPlay.Api.Models;

public class Match
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid LeagueId { get; set; }
    
    public DateTime Date { get; set; }
    
    [MaxLength(200)]
    public string? Location { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string FormatType { get; set; } = "8v8";
    
    public bool IsCompleted { get; set; }
    
    // Navigation properties
    public League? League { get; set; }
    public List<MatchAssignment> MatchAssignments { get; set; } = new();
    public List<RawRating> RawRatings { get; set; } = new();
}

using System.ComponentModel.DataAnnotations;

namespace FairPlay.Api.Models;

public class Match
{
    public Guid Id { get; set; }
    
    public DateTime Date { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string FormatType { get; set; } = "8v8";
    
    public bool IsCompleted { get; set; }
    
    public List<MatchAssignment> MatchAssignments { get; set; } = new();
    public List<RawRating> RawRatings { get; set; } = new();
}

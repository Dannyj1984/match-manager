using System.ComponentModel.DataAnnotations;

namespace FairPlay.Api.Models;

public class RawRating
{
    public Guid Id { get; set; }
    
    public Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;
    
    public Guid RaterId { get; set; }
    public Player Rater { get; set; } = null!;
    
    public Guid SubjectId { get; set; }
    public Player Subject { get; set; } = null!;
    
    [Range(1, 10)]
    public int Value { get; set; }
}

namespace FairPlay.Api.Models;

public class PlayerRating
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid RaterId { get; set; }
    public Guid RatedPlayerId { get; set; }
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Match? Match { get; set; }
    public Player? Rater { get; set; }
    public Player? RatedPlayer { get; set; }
}

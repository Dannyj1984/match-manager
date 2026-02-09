
namespace FairPlay.Api.DTOs;

public class PlayerStatsDto
{
    public int GamesPlayed { get; set; }
    public int CurrentStreak { get; set; }
    public decimal? HighestRating4Weeks { get; set; }
    public bool IsGoldenBoot { get; set; }
    public int EarlyBirdCount { get; set; }
}

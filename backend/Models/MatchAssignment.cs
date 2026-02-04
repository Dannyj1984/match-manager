using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FairPlay.Api.Models;

public class MatchAssignment
{
    public Guid MatchId { get; set; }
    [JsonIgnore]
    public Match Match { get; set; } = null!;
    
    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    
    public int TeamNumber { get; set; }
}

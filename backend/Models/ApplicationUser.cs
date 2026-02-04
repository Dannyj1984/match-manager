using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace FairPlay.Api.Models;

public class ApplicationUser : IdentityUser
{
    // Link to the Player entity
    public Guid? PlayerId { get; set; }

    [JsonIgnore]
    public Player? Player { get; set; }
}

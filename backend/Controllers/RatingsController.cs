using FairPlay.Api.Data;
using FairPlay.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace FairPlay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RatingsController : ControllerBase
{
    private readonly FairPlayDbContext _context;

    public RatingsController(FairPlayDbContext context)
    {
        _context = context;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitRatings([FromBody] List<RawRating> ratings)
    {
        if (ratings == null || !ratings.Any()) return BadRequest("No ratings provided.");

        _context.RawRatings.AddRange(ratings);
        await _context.SaveChangesAsync();
        return Ok(new { Count = ratings.Count });
    }
}

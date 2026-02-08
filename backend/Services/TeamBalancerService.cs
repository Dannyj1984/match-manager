using FairPlay.Api.Models;

namespace FairPlay.Api.Services;

public interface ITeamBalancerService
{
    List<MatchAssignment> BalanceTeams(Guid matchId, List<Player> players, int teamCount, string sport);
}

public class TeamBalancerService : ITeamBalancerService
{
    public List<MatchAssignment> BalanceTeams(Guid matchId, List<Player> players, int teamCount, string sport)
    {
        if (teamCount <= 0) throw new ArgumentException("Team count must be greater than zero.");
        
        var assignments = new List<MatchAssignment>();
        var sportPositions = GetPositionsForSport(sport);
        
        // Track assigned players
        var assignedPlayerIds = new HashSet<Guid>();
        
        // Track team stats
        var teamRatings = new Dictionary<int, decimal>();
        var teamCounts = new Dictionary<int, int>();
        
        for (int i = 1; i <= teamCount; i++)
        {
            teamRatings[i] = 0;
            teamCounts[i] = 0;
        }

        // Phase 1: Assign by Position (The "Pot" approach)
        foreach (var position in sportPositions)
        {
            // 1. Create a "Pot" of candidates for this position
            // Prioritize "Specialists" (players who ONLY play this position)
            var candidates = players
                .Where(p => !assignedPlayerIds.Contains(p.Id) && p.PreferredPosition.Contains(position))
                .OrderBy(p => p.PreferredPosition.Count) // Specialists first (Count == 1)
                .ThenByDescending(p => p.CurrentRating)  // Then strongest players
                .Take(teamCount) // Only take enough to fill this slot for each team
                .ToList();

            if (!candidates.Any()) continue;

            // 2. Prepare for assignment: Sort candidates by strength
            var playersToAssign = candidates.OrderByDescending(p => p.CurrentRating).ToList();

            // 3. Assign each player specifically to the best fitting team
            foreach (var player in playersToAssign)
            {
                // Find best team: 
                // Priority 1: Fewest Players (Force even teams)
                // Priority 2: Lowest Rating (Balance skill)
                var bestTeam = teamCounts.Keys
                    .OrderBy(t => teamCounts[t])      // Fill empty/smaller teams first
                    .ThenBy(t => teamRatings[t])      // Then balance by rating
                    .First();

                assignments.Add(new MatchAssignment
                {
                    MatchId = matchId,
                    PlayerId = player.Id,
                    TeamNumber = bestTeam
                });

                teamCounts[bestTeam]++;
                teamRatings[bestTeam] += player.CurrentRating;
                assignedPlayerIds.Add(player.Id);
            }
        }

        // Phase 2: Assign remaining players
        var remainingPlayers = players
            .Where(p => !assignedPlayerIds.Contains(p.Id))
            .OrderByDescending(p => p.CurrentRating)
            .ToList();

        foreach (var player in remainingPlayers)
        {
            // Same logic: Priority 1 Size, Priority 2 Rating
            var bestTeam = teamCounts.Keys
                .OrderBy(t => teamCounts[t])
                .ThenBy(t => teamRatings[t])
                .First();

            assignments.Add(new MatchAssignment
            {
                MatchId = matchId,
                PlayerId = player.Id,
                TeamNumber = bestTeam
            });

            teamCounts[bestTeam]++;
            teamRatings[bestTeam] += player.CurrentRating;
            assignedPlayerIds.Add(player.Id);
        }
        
        return assignments;
    }

    private List<string> GetPositionsForSport(string sport)
    {
        return sport switch
        {
            "Football" => new List<string> { "Goalkeeper", "Defender", "Midfielder", "Forward" },
            "Netball" => new List<string> { "GK", "GD", "WD", "C", "WA", "GA", "GS" },
            "Basketball" => new List<string> { "Point Guard", "Shooting Guard", "Small Forward", "Power Forward", "Center" },
            "Rugby" => new List<string> { "Prop", "Hooker", "Lock", "Flanker", "Number 8", "Scrum-half", "Fly-half", "Centre", "Winger", "Fullback" },
            _ => new List<string>() // Default: no specific positions
        };
    }
}

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
        
        // Get the positions for this sport
        var sportPositions = GetPositionsForSport(sport);
        
        // Group players by their preferred position
        var playersByPosition = players
            .GroupBy(p => p.PreferredPosition)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.CurrentRating).ToList());
        
        // Track team assignments
        var teamAssignments = new Dictionary<int, List<Player>>();
        for (int i = 1; i <= teamCount; i++)
        {
            teamAssignments[i] = new List<Player>();
        }
        
        // Phase 1: Distribute one player of each position to each team (round-robin)
        foreach (var position in sportPositions)
        {
            if (!playersByPosition.ContainsKey(position)) continue;
            
            var positionPlayers = playersByPosition[position];
            int teamIndex = 1;
            
            foreach (var player in positionPlayers)
            {
                assignments.Add(new MatchAssignment
                {
                    MatchId = matchId,
                    PlayerId = player.Id,
                    TeamNumber = teamIndex
                });
                
                teamAssignments[teamIndex].Add(player);
                
                teamIndex++;
                if (teamIndex > teamCount) break; // Only assign one per team in this phase
            }
        }
        
        // Phase 2: Distribute remaining players (those that didn't get assigned in phase 1)
        var assignedPlayerIds = new HashSet<Guid>(assignments.Select(a => a.PlayerId));
        var remainingPlayers = players
            .Where(p => !assignedPlayerIds.Contains(p.Id))
            .OrderByDescending(p => p.CurrentRating)
            .ToList();
        
        // Use snake draft for remaining players to balance team strength
        bool leftToRight = true;
        int currentTeam = 1;
        
        foreach (var player in remainingPlayers)
        {
            assignments.Add(new MatchAssignment
            {
                MatchId = matchId,
                PlayerId = player.Id,
                TeamNumber = currentTeam
            });
            
            teamAssignments[currentTeam].Add(player);
            
            if (leftToRight)
            {
                if (currentTeam < teamCount) currentTeam++;
                else { currentTeam = teamCount; leftToRight = false; }
            }
            else
            {
                if (currentTeam > 1) currentTeam--;
                else { currentTeam = 1; leftToRight = true; }
            }
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

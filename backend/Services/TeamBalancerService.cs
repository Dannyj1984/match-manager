using FairPlay.Api.Models;

namespace FairPlay.Api.Services;

public interface ITeamBalancerService
{
    List<MatchAssignment> BalanceTeams(Guid matchId, List<Player> players, int teamCount);
}

public class TeamBalancerService : ITeamBalancerService
{
    public List<MatchAssignment> BalanceTeams(Guid matchId, List<Player> players, int teamCount)
    {
        if (teamCount <= 0) throw new ArgumentException("Team count must be greater than zero.");
        
        var assignments = new List<MatchAssignment>();
        var remainingPlayers = players.OrderByDescending(p => p.CurrentRating).ToList();
        
        // 1. Prioritize Goalkeepers for even distribution
        var gks = remainingPlayers.Where(p => p.PreferredPosition == "GK").ToList();
        remainingPlayers.RemoveAll(p => p.PreferredPosition == "GK");
        
        int currentTeamToAssign = 1;
        foreach (var gk in gks)
        {
            assignments.Add(new MatchAssignment
            {
                MatchId = matchId,
                PlayerId = gk.Id,
                TeamNumber = currentTeamToAssign
            });
            currentTeamToAssign = (currentTeamToAssign % teamCount) + 1;
        }

        // 2. Distribute remaining players using snake draft to balance power
        // We continue from where we left off or reset? 
        // Better to reset snake or track per-team current counts?
        // Let's use a standard snake draft for the rest to ensure rating balance.
        
        var teams = new List<List<Player>>();
        for (int i = 0; i < teamCount; i++) teams.Add(new List<Player>());
        
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

            if (leftToRight)
            {
                if (currentTeam < teamCount) currentTeam++;
                else leftToRight = false;
            }
            else
            {
                if (currentTeam > 1) currentTeam--;
                else leftToRight = true;
            }
        }

        return assignments;
    }
}

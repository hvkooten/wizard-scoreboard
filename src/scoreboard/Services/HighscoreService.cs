using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

public class HighscoreService : IHighscoreService
{
    private readonly List<Player> leaderboard = new();

    public IEnumerable<Player> GetHighscores() => leaderboard.OrderByDescending(p => p.Wins).ThenByDescending(p => p.HighestScore);

    public void UpdateHighscores(IEnumerable<Group> groups)
    {
        leaderboard.Clear();
        foreach (var group in groups)
        {
            foreach (var player in group.Players)
            {
                var existing = leaderboard.FirstOrDefault(p => p.Id == player.Id);
                if (existing == null)
                {
                    leaderboard.Add(new Player
                    {
                        Id = player.Id,
                        Name = player.Name,
                        Wins = player.Wins,
                        HighestScore = player.HighestScore
                    });
                }
                else
                {
                    existing.Wins = Math.Max(existing.Wins, player.Wins);
                    existing.HighestScore = Math.Max(existing.HighestScore, player.HighestScore);
                }
            }
        }
    }
}

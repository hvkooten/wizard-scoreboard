using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

public class HighscoreService : IHighscoreService
{
    private readonly List<Player> leaderboard = new();

    public IEnumerable<Player> GetHighscores() => leaderboard
        .OrderByDescending(p => p.Wins)
        .ThenByDescending(p => p.HighestScore)
        .ThenByDescending(p => p.GamesPlayed);

    private static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

    public void UpdateHighscores(IEnumerable<Group> groups)
    {
        leaderboard.Clear();
        foreach (var group in groups)
        {
            foreach (var player in group.Players)
            {
                var existing = leaderboard.FirstOrDefault(p => NormalizeName(p.Name) == NormalizeName(player.Name));
                if (existing == null)
                {
                    leaderboard.Add(new Player
                    {
                        Id = player.Id,
                        Name = player.Name,
                        Wins = player.Wins,
                        GamesPlayed = player.GamesPlayed,
                        HighestScore = player.HighestScore
                    });
                }
                else
                {
                    existing.Wins += player.Wins;
                    existing.GamesPlayed += player.GamesPlayed;
                    existing.HighestScore = Math.Max(existing.HighestScore, player.HighestScore);
                }
            }
        }
    }
}

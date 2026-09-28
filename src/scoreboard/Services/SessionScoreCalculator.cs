using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

/// <summary>
/// Recalculates cumulative per-player scores for a saved session from its recorded rounds.
/// </summary>
public static class SessionScoreCalculator
{
    /// <summary>
    /// Computes the total points per player by replaying the session's rounds.
    /// A correct bid scores 2 points plus 1 point per trick won; a wrong bid scores
    /// minus 1 point per trick difference. Rounds without recorded actual tricks are ignored.
    /// </summary>
    public static Dictionary<Guid, int> CalculateSessionScores(ScoreSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var totals = session.Players.ToDictionary(p => p.Id, _ => 0);

        foreach (var round in session.Rounds.OrderBy(r => r.RoundNumber))
        {
            foreach (var player in session.Players)
            {
                var bid = round.BidByPlayer.GetValueOrDefault(player.Id, -1);
                var actual = round.ActualByPlayer.GetValueOrDefault(player.Id, -1);

                if (actual < 0)
                {
                    continue;
                }

                var delta = bid == actual
                    ? 2 + actual
                    : -Math.Abs(bid - actual);
                totals[player.Id] += delta;
            }
        }

        return totals;
    }
}

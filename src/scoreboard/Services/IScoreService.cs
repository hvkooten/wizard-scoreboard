using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

public interface IScoreService
{
    ScoreSession StartGame(Group group);
    ScoreSession StartGame(Group group, List<Player> players);
    void EndGame(ScoreSession session);
    RoundEntry StartRound(ScoreSession session, TrumpSuit trump, Dictionary<Guid, int> bids);
    void FinishRound(ScoreSession session, Dictionary<Guid, int> actuals);
    IEnumerable<ScoreSession> GetActiveSessions();
}

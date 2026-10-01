using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

public interface IScoreService
{
    ScoreSession StartGame(Group group);
    ScoreSession StartGame(Group group, List<Player> players);
    void PauseGame(ScoreSession session);
    void ResumeGame(ScoreSession session);
    void EndGame(ScoreSession session);
    RoundEntry StartRound(ScoreSession session, TrumpSuit trump, Dictionary<Guid, int> bids);
    void CancelRound(ScoreSession session);
    void FinishRound(ScoreSession session, Dictionary<Guid, int> actuals);
    /// <summary>
    /// Corrects tricks won in the latest completed round of an active, unpaused game without changing bids.
    /// </summary>
    void UpdateLastRoundActuals(ScoreSession session, Dictionary<Guid, int> actuals);
    IEnumerable<ScoreSession> GetActiveSessions();
    IEnumerable<ScoreSession> GetSavedGames();
    void DeleteSavedGame(Guid sessionId);
    ScoreSession? GetCurrentSession();
    void SelectSavedGame(Guid sessionId);
}

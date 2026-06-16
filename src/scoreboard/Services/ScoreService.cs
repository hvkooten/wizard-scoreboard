using Microsoft.Maui.Storage;
using System.Text.Json;
using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

public class ScoreService : IScoreService
{
    private const string PausedSessionsStorageKey = "paused_sessions_storage_v1";
    private readonly List<ScoreSession> sessions = new();

    public ScoreService()
    {
        LoadPausedSessions();
    }

    private static int GetMaxRounds(int playerCount)
    {
        return playerCount switch
        {
            3 => 20,
            4 => 15,
            5 => 12,
            6 => 10,
            _ => throw new ArgumentOutOfRangeException(nameof(playerCount), "Moet 3-6 spelers zijn."),
        };
    }

    public ScoreSession StartGame(Group group) => StartGame(group, group.Players.ToList());

    public ScoreSession StartGame(Group group, List<Player> players)
    {
        if (players.Count < 3 || players.Count > 6)
            throw new ArgumentException("Aantal spelers moet tussen 3 en 6 liggen.");

        // Use group-specific setting; fallback to player count for migrated groups.
        var bidTotalRuleStartRound = group.BidTotalRuleStartRound is >= 0 and <= 13
            ? group.BidTotalRuleStartRound
            : players.Count;

        var session = new ScoreSession
        {
            GroupId = group.Id,
            Players = players.ToList(),
            MaxRounds = GetMaxRounds(players.Count),
            IsActive = true,
            BidTotalRuleStartRound = bidTotalRuleStartRound
        };

        sessions.Add(session);
        SavePausedSessions();
        return session;
    }

    public void PauseGame(ScoreSession session)
    {
        if (!session.IsActive)
            throw new InvalidOperationException("Spelsessie is niet actief.");

        session.IsPaused = true;
        SavePausedSessions();
    }

    public void ResumeGame(ScoreSession session)
    {
        if (!session.IsActive)
            throw new InvalidOperationException("Spelsessie is niet actief.");

        session.IsPaused = false;
        SavePausedSessions();
    }

    public void EndGame(ScoreSession session)
    {
        if (!session.IsActive)
            return;

        if (session.Rounds.Count > 0 && session.Players.Count > 0)
        {
            var bestScore = session.Players.Max(p => p.CurrentPoints);
            var winners = session.Players.Where(p => p.CurrentPoints == bestScore);
            foreach (var winner in winners)
            {
                winner.Wins += 1;
            }
        }

        session.IsPaused = false;
        session.IsActive = false;
        SavePausedSessions();
    }

    public RoundEntry StartRound(ScoreSession session, TrumpSuit trump, Dictionary<Guid, int> bids)
    {
        if (!session.IsActive)
            throw new InvalidOperationException("Spelsessie is niet actief.");

        if (session.IsPaused)
            throw new InvalidOperationException("Spelsessie is gepauzeerd.");

        if (session.CurrentRound >= session.MaxRounds)
            throw new InvalidOperationException("Geen rondes meer beschikbaar.");

        if (bids.Any(b => b.Value < 0 || b.Value > session.CurrentRound + 1))
            throw new ArgumentOutOfRangeException(nameof(bids), "Voorspelde slagen moeten tussen 0 en ronde nummer liggen.");

        session.CurrentRound++;
        session.Trump = trump;

        var increment = (session.CurrentRound - 1) % session.Players.Count;
        var dealer = session.Players[increment];

        var round = new RoundEntry
        {
            RoundNumber = session.CurrentRound,
            DealerPlayerId = dealer.Id,
            BidByPlayer = new Dictionary<Guid, int>(bids),
            Trump = trump,
        };

        session.Rounds.Add(round);
        SavePausedSessions();
        return round;
    }

    public void FinishRound(ScoreSession session, Dictionary<Guid, int> actuals)
    {
        var round = session.Rounds.LastOrDefault();
        if (round == null)
            throw new InvalidOperationException("Geen actieve ronde om af te sluiten.");

        if (actuals.Any(a => a.Value < 0 || a.Value > round.RoundNumber))
            throw new ArgumentOutOfRangeException(nameof(actuals), "Werkelijke slagen moeten tussen 0 en ronde nummer liggen.");

        round.ActualByPlayer = new Dictionary<Guid, int>(actuals);

        foreach (var player in session.Players)
        {
            var bid = round.BidByPlayer.GetValueOrDefault(player.Id);
            var actual = actuals.GetValueOrDefault(player.Id);
            // Correct: 2 points + 1 point per trick won.
            // Wrong:   -1 point per trick difference.
            var scoreDelta = bid == actual
                ? 2 + actual
                : -Math.Abs(bid - actual);
            player.CurrentPoints += scoreDelta;

            if (player.CurrentPoints > player.HighestScore)
                player.HighestScore = player.CurrentPoints;
        }

        if (session.CurrentRound >= session.MaxRounds)
        {
            EndGame(session);
        }

        SavePausedSessions();
    }

    public IEnumerable<ScoreSession> GetActiveSessions() => sessions.Where(s => s.IsActive);

    private void LoadPausedSessions()
    {
        string raw;
        try
        {
            raw = Preferences.Default.Get(PausedSessionsStorageKey, string.Empty);
        }
        catch (Exception)
        {
            // Preferences may be unavailable in non-MAUI contexts (for example unit tests).
            return;
        }

        if (string.IsNullOrWhiteSpace(raw))
            return;

        try
        {
            var saved = JsonSerializer.Deserialize<List<ScoreSession>>(raw);
            if (saved == null)
                return;

            sessions.Clear();
            sessions.AddRange(saved.Where(s => s.IsActive && s.IsPaused));
        }
        catch (JsonException)
        {
            sessions.Clear();
        }
    }

    private void SavePausedSessions()
    {
        var paused = sessions.Where(s => s.IsActive && s.IsPaused).ToList();
        var raw = JsonSerializer.Serialize(paused);
        try
        {
            Preferences.Default.Set(PausedSessionsStorageKey, raw);
        }
        catch (Exception)
        {
            // Ignore persistence failures outside app runtime.
        }
    }
}

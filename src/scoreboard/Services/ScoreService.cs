using System.Text.Json;
using WizardScoreboard.Models;
using WizardScoreboard.Resources;

namespace WizardScoreboard.Services;

public class ScoreService : IScoreService
{
    private const string PausedSessionsStorageKey = "paused_sessions_storage_v1";
    private readonly IPreferences preferences;
    private readonly List<ScoreSession> sessions = new();
    private Guid? selectedSessionId;

    /// <summary>Creates the score service using device preferences.</summary>
    public ScoreService() : this(Preferences.Default)
    {
    }

    /// <summary>Creates the score service using the supplied persistent preferences.</summary>
    public ScoreService(IPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        this.preferences = preferences;
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

        // Start each game with a clean per-game score, while preserving long-term stats.
        var sessionPlayers = players
            .OrderBy(p => p.Order)
            .Select(p => new Player
            {
                Id = p.Id,
                Name = p.Name,
                Wins = p.Wins,
                GamesPlayed = p.GamesPlayed,
                HighestScore = p.HighestScore,
                Order = p.Order,
                CurrentPoints = 0
            })
            .ToList();

        // Use group-specific setting; fallback to player count for migrated groups.
        var bidTotalRuleStartRound = group.BidTotalRuleStartRound switch
        {
            >= 0 and <= Group.MaxRoundLimit => group.BidTotalRuleStartRound,
            Group.DoublePlayerCountRule => Group.PlayerCountStartRound(sessionPlayers.Count, doublePlayerCount: true),
            _ => Group.PlayerCountStartRound(sessionPlayers.Count, doublePlayerCount: false)
        };

        var session = new ScoreSession
        {
            GroupId = group.Id,
            Players = sessionPlayers,
            MaxRounds = GetMaxRounds(sessionPlayers.Count),
            IsActive = true,
            BidTotalRuleStartRound = bidTotalRuleStartRound
        };

        sessions.Add(session);
        selectedSessionId = session.Id;
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
            foreach (var player in session.Players)
            {
                player.GamesPlayed += 1;
            }

            var bestScore = session.Players.Max(p => p.CurrentPoints);
            var winners = session.Players.Where(p => p.CurrentPoints == bestScore);
            foreach (var winner in winners)
            {
                winner.Wins += 1;
            }
        }

        session.IsPaused = false;
        session.IsActive = false;
        if (selectedSessionId == session.Id)
        {
            selectedSessionId = null;
        }
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
            HighestScoreBeforeRoundByPlayer = session.Players.ToDictionary(p => p.Id, p => p.HighestScore),
            Trump = trump,
        };

        session.Rounds.Add(round);
        SavePausedSessions();
        return round;
    }

    public void CancelRound(ScoreSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var round = session.Rounds.LastOrDefault();
        if (round == null)
            throw new InvalidOperationException("Geen actieve ronde om te annuleren.");

        if (round.ActualByPlayer.Count > 0)
            throw new InvalidOperationException("Ronde is al afgesloten en kan niet meer geannuleerd worden.");

        session.Rounds.Remove(round);
        session.CurrentRound--;
        session.Trump = session.Rounds.LastOrDefault()?.Trump ?? TrumpSuit.None;
        SavePausedSessions();
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

    /// <inheritdoc />
    public void UpdateLastRoundActuals(ScoreSession session, Dictionary<Guid, int> actuals)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(actuals);

        var round = session.Rounds.LastOrDefault();
        if (!session.IsActive || session.IsPaused || round == null
            || round.RoundNumber != session.CurrentRound || round.ActualByPlayer.Count == 0)
        {
            throw new InvalidOperationException(Localization.GetString("RoundCannotBeEdited"));
        }

        if (actuals.Count != session.Players.Count || session.Players.Any(p => !actuals.ContainsKey(p.Id)))
        {
            throw new ArgumentException(Localization.GetString("ActualsMustIncludeAllPlayers"), nameof(actuals));
        }

        if (actuals.Any(a => a.Value < 0 || a.Value > round.RoundNumber))
        {
            throw new ArgumentOutOfRangeException(nameof(actuals),
                string.Format(Localization.GetString("ActualsRangeError"), round.RoundNumber));
        }

        if (actuals.Values.Sum() != round.RoundNumber)
        {
            throw new ArgumentException(
                string.Format(Localization.GetString("TotalActualsError"), round.RoundNumber), nameof(actuals));
        }

        round.ActualByPlayer = new Dictionary<Guid, int>(actuals);
        var totals = SessionScoreCalculator.CalculateSessionScores(session);
        foreach (var player in session.Players)
        {
            player.CurrentPoints = totals[player.Id];
            // Older saved rounds lack a snapshot, so preserve their historical record.
            var previousHighestScore = round.HighestScoreBeforeRoundByPlayer.GetValueOrDefault(player.Id, player.HighestScore);
            player.HighestScore = Math.Max(previousHighestScore, player.CurrentPoints);
        }

        SavePausedSessions();
    }

    public IEnumerable<ScoreSession> GetActiveSessions() => sessions.Where(s => s.IsActive);

    public IEnumerable<ScoreSession> GetSavedGames()
    {
        return sessions
            .Where(s => s.IsActive && s.IsPaused)
            .OrderByDescending(s => s.StartDate);
    }

    public void DeleteSavedGame(Guid sessionId)
    {
        var found = sessions.FirstOrDefault(s => s.Id == sessionId && s.IsActive && s.IsPaused);
        if (found == null)
        {
            return;
        }

        sessions.Remove(found);

        if (selectedSessionId == sessionId)
        {
            selectedSessionId = null;
        }

        SavePausedSessions();
    }

    public ScoreSession? GetCurrentSession()
    {
        if (selectedSessionId.HasValue)
        {
            var selected = sessions.FirstOrDefault(s => s.Id == selectedSessionId.Value && s.IsActive);
            if (selected != null)
            {
                return selected;
            }
        }

        var latest = sessions
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        if (latest != null)
        {
            selectedSessionId = latest.Id;
        }

        return latest;
    }

    public void SelectSavedGame(Guid sessionId)
    {
        var found = sessions.FirstOrDefault(s => s.Id == sessionId && s.IsActive && s.IsPaused);
        if (found != null)
        {
            selectedSessionId = found.Id;
        }
    }

    private void LoadPausedSessions()
    {
        string raw;
        try
        {
            raw = preferences.Get(PausedSessionsStorageKey, string.Empty);
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
            // Games that were still in progress when the app closed are restored as saved games.
            foreach (var session in saved.Where(s => s.IsActive))
            {
                session.IsPaused = true;
                sessions.Add(session);
            }
            selectedSessionId = sessions
                .OrderByDescending(s => s.StartDate)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefault();
        }
        catch (JsonException)
        {
            sessions.Clear();
            selectedSessionId = null;
        }
    }

    private void SavePausedSessions()
    {
        var active = sessions.Where(s => s.IsActive).ToList();
        var raw = JsonSerializer.Serialize(active);
        try
        {
            preferences.Set(PausedSessionsStorageKey, raw);
        }
        catch (Exception)
        {
            // Ignore persistence failures outside app runtime.
        }
    }
}

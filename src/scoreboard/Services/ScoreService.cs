using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

public class ScoreService : IScoreService
{
    private readonly List<ScoreSession> sessions = new();

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

    public ScoreSession StartGame(Group group)
    {
        if (group.Players.Count < 3 || group.Players.Count > 6)
            throw new ArgumentException("Aantal spelers moet tussen 3 en 6 liggen.");

        var session = new ScoreSession
        {
            GroupId = group.Id,
            Players = group.Players.ToList(),
            MaxRounds = GetMaxRounds(group.Players.Count),
            IsActive = true
        };

        sessions.Add(session);
        return session;
    }

    public void EndGame(ScoreSession session)
    {
        session.IsActive = false;
        session.CurrentRound = 0;
    }

    public RoundEntry StartRound(ScoreSession session, TrumpSuit trump, Dictionary<Guid, int> bids)
    {
        if (!session.IsActive)
            throw new InvalidOperationException("Spelsessie is niet actief.");

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
            var scoreDelta = bid == actual ? 10 + bid * 2 : -Math.Abs(bid - actual) * 5;
            player.CurrentPoints += scoreDelta;

            if (player.CurrentPoints > player.HighestScore)
                player.HighestScore = player.CurrentPoints;

            if (player.CurrentPoints >= 100)
                player.Wins += 1;
        }

        if (session.CurrentRound >= session.MaxRounds)
        {
            EndGame(session);
        }
    }

    public IEnumerable<ScoreSession> GetActiveSessions() => sessions.Where(s => s.IsActive);
}

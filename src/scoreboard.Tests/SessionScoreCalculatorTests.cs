using System;
using System.Collections.Generic;
using NUnit.Framework;
using WizardScoreboard.Models;
using WizardScoreboard.Services;

namespace WizardScoreboard.Tests;

public class SessionScoreCalculatorTests
{
    private static Player CreatePlayer(string name, int order)
    {
        return new Player { Name = name, Order = order };
    }

    private static ScoreSession CreateSession(params Player[] players)
    {
        return new ScoreSession { Players = new List<Player>(players) };
    }

    [Test]
    public void CalculateSessionScores_NoRounds_ReturnsZeroForEachPlayer()
    {
        var a = CreatePlayer("A", 0);
        var b = CreatePlayer("B", 1);
        var session = CreateSession(a, b);

        var scores = SessionScoreCalculator.CalculateSessionScores(session);

        Assert.AreEqual(0, scores[a.Id]);
        Assert.AreEqual(0, scores[b.Id]);
    }

    [Test]
    public void CalculateSessionScores_CorrectBid_ScoresTwoPlusTricks()
    {
        var a = CreatePlayer("A", 0);
        var session = CreateSession(a);
        session.Rounds.Add(new RoundEntry
        {
            RoundNumber = 3,
            BidByPlayer = new Dictionary<Guid, int> { [a.Id] = 3 },
            ActualByPlayer = new Dictionary<Guid, int> { [a.Id] = 3 }
        });

        var scores = SessionScoreCalculator.CalculateSessionScores(session);

        Assert.AreEqual(5, scores[a.Id]);
    }

    [Test]
    public void CalculateSessionScores_CorrectZeroBid_ScoresTwoPoints()
    {
        var a = CreatePlayer("A", 0);
        var session = CreateSession(a);
        session.Rounds.Add(new RoundEntry
        {
            RoundNumber = 1,
            BidByPlayer = new Dictionary<Guid, int> { [a.Id] = 0 },
            ActualByPlayer = new Dictionary<Guid, int> { [a.Id] = 0 }
        });

        var scores = SessionScoreCalculator.CalculateSessionScores(session);

        Assert.AreEqual(2, scores[a.Id]);
    }

    [Test]
    public void CalculateSessionScores_WrongBid_ScoresNegativeDifference()
    {
        var a = CreatePlayer("A", 0);
        var session = CreateSession(a);
        session.Rounds.Add(new RoundEntry
        {
            RoundNumber = 5,
            BidByPlayer = new Dictionary<Guid, int> { [a.Id] = 4 },
            ActualByPlayer = new Dictionary<Guid, int> { [a.Id] = 1 }
        });

        var scores = SessionScoreCalculator.CalculateSessionScores(session);

        Assert.AreEqual(-3, scores[a.Id]);
    }

    [Test]
    public void CalculateSessionScores_RoundWithoutActuals_IsIgnored()
    {
        var a = CreatePlayer("A", 0);
        var session = CreateSession(a);
        session.Rounds.Add(new RoundEntry
        {
            RoundNumber = 2,
            BidByPlayer = new Dictionary<Guid, int> { [a.Id] = 2 },
            ActualByPlayer = new Dictionary<Guid, int>()
        });

        var scores = SessionScoreCalculator.CalculateSessionScores(session);

        Assert.AreEqual(0, scores[a.Id]);
    }

    [Test]
    public void CalculateSessionScores_MultipleRounds_AccumulatesTotals()
    {
        var a = CreatePlayer("A", 0);
        var session = CreateSession(a);
        session.Rounds.Add(new RoundEntry
        {
            RoundNumber = 1,
            BidByPlayer = new Dictionary<Guid, int> { [a.Id] = 1 },
            ActualByPlayer = new Dictionary<Guid, int> { [a.Id] = 1 } // +3
        });
        session.Rounds.Add(new RoundEntry
        {
            RoundNumber = 2,
            BidByPlayer = new Dictionary<Guid, int> { [a.Id] = 2 },
            ActualByPlayer = new Dictionary<Guid, int> { [a.Id] = 0 } // -2
        });

        var scores = SessionScoreCalculator.CalculateSessionScores(session);

        Assert.AreEqual(1, scores[a.Id]);
    }

    [Test]
    public void CalculateSessionScores_MissingBid_TreatedAsWrongBid()
    {
        var a = CreatePlayer("A", 0);
        var session = CreateSession(a);
        session.Rounds.Add(new RoundEntry
        {
            RoundNumber = 2,
            BidByPlayer = new Dictionary<Guid, int>(),
            ActualByPlayer = new Dictionary<Guid, int> { [a.Id] = 2 }
        });

        var scores = SessionScoreCalculator.CalculateSessionScores(session);

        // Bid defaults to -1, actual 2 -> -|(-1) - 2| = -3
        Assert.AreEqual(-3, scores[a.Id]);
    }

    [Test]
    public void CalculateSessionScores_NullSession_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SessionScoreCalculator.CalculateSessionScores(null!));
    }
}

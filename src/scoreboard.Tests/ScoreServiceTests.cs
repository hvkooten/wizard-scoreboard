using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WizardScoreboard.Models;
using WizardScoreboard.Services;

namespace WizardScoreboard.Tests;

public class ScoreServiceTests
{
    [Test]
    public void StartGame_CreatesSession_WithCorrectMaxRoundsForFourPlayers()
    {
        var scoreService = new ScoreService();

        var players = new List<Player>
        {
            new Player { Name = "A", Order=0 },
            new Player { Name = "B", Order=1 },
            new Player { Name = "C", Order=2 },
            new Player { Name = "D", Order=3 }
        };

        var group = new Group { Name = "Test", Players = players };
        var session = scoreService.StartGame(group);

        Assert.NotNull(session);
        Assert.AreEqual(15, session.MaxRounds);
        Assert.IsTrue(session.IsActive);
    }

    [Test]
    public void StartRound_AndFinish_UpdatesPlayerScores()
    {
        var scoreService = new ScoreService();

        var players = new List<Player>
        {
            new Player { Name = "A", Order=0 },
            new Player { Name = "B", Order=1 },
            new Player { Name = "C", Order=2 }
        };

        var group = new Group { Name = "Test", Players = players };
        var session = scoreService.StartGame(group);

        var bids = session.Players.ToDictionary(p => p.Id, p => 1);

        var round = scoreService.StartRound(session, TrumpSuit.Hearts, bids);
        scoreService.FinishRound(session, bids);

        Assert.AreEqual(1, session.CurrentRound);
        Assert.IsTrue(session.Players.All(p => p.CurrentPoints != 0));
    }

    [Test]
    public void StartGame_UsesGroupBidTotalRuleStartRound_WhenValid()
    {
        var scoreService = new ScoreService();

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 },
            new Player { Name = "D", Order = 3 }
        };

        var group = new Group
        {
            Name = "RuleGroup",
            Players = players,
            BidTotalRuleStartRound = 6
        };

        var session = scoreService.StartGame(group);

        Assert.AreEqual(6, session.BidTotalRuleStartRound);
    }

    [Test]
    public void StartGame_UsesDisabledBidRule_WhenGroupValueIsZero()
    {
        var scoreService = new ScoreService();

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group
        {
            Name = "DisabledRuleGroup",
            Players = players,
            BidTotalRuleStartRound = 0
        };

        var session = scoreService.StartGame(group);

        Assert.AreEqual(0, session.BidTotalRuleStartRound);
    }

    [Test]
    public void StartGame_FallsBackToPlayerCount_WhenGroupValueIsInvalid()
    {
        var scoreService = new ScoreService();

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 },
            new Player { Name = "D", Order = 3 },
            new Player { Name = "E", Order = 4 }
        };

        var group = new Group
        {
            Name = "FallbackGroup",
            Players = players,
            BidTotalRuleStartRound = -1
        };

        var session = scoreService.StartGame(group);

        Assert.AreEqual(players.Count, session.BidTotalRuleStartRound);
    }

    [Test]
    public void FinishRound_OnFinalRound_EndsGameAndKeepsFinalRoundNumber()
    {
        var scoreService = new ScoreService();

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group { Name = "FinalRoundGroup", Players = players };
        var session = scoreService.StartGame(group);
        session.MaxRounds = 1;

        var bids = session.Players.ToDictionary(p => p.Id, _ => 0);

        scoreService.StartRound(session, TrumpSuit.Hearts, bids);
        scoreService.FinishRound(session, bids);

        Assert.AreEqual(1, session.CurrentRound);
        Assert.IsFalse(session.IsActive);
    }

    [Test]
    public void EndGame_EarlyStop_AssignsWinToHighestScorePlayer()
    {
        var scoreService = new ScoreService();

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group { Name = "EarlyStopGroup", Players = players };
        var session = scoreService.StartGame(group);

        var bids = session.Players.ToDictionary(p => p.Id, _ => 0);
        scoreService.StartRound(session, TrumpSuit.Hearts, bids);

        session.Players[0].CurrentPoints = 12;
        session.Players[1].CurrentPoints = 8;
        session.Players[2].CurrentPoints = 3;

        scoreService.EndGame(session);

        Assert.AreEqual(1, session.Players[0].Wins);
        Assert.AreEqual(0, session.Players[1].Wins);
        Assert.AreEqual(0, session.Players[2].Wins);
    }

    [Test]
    public void EndGame_Tie_AssignsWinToAllTopPlayers()
    {
        var scoreService = new ScoreService();

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group { Name = "TieGroup", Players = players };
        var session = scoreService.StartGame(group);

        var bids = session.Players.ToDictionary(p => p.Id, _ => 0);
        scoreService.StartRound(session, TrumpSuit.Hearts, bids);

        // Force a tie at top between A and B.
        session.Players[0].CurrentPoints = 10;
        session.Players[1].CurrentPoints = 10;
        session.Players[2].CurrentPoints = 7;

        scoreService.EndGame(session);

        Assert.AreEqual(1, session.Players[0].Wins);
        Assert.AreEqual(1, session.Players[1].Wins);
        Assert.AreEqual(0, session.Players[2].Wins);
    }

    [Test]
    public void EndGame_WithoutRounds_DoesNotAssignWins()
    {
        var scoreService = new ScoreService();

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group { Name = "NoRoundsGroup", Players = players };
        var session = scoreService.StartGame(group);

        scoreService.EndGame(session);

        Assert.IsTrue(session.Players.All(p => p.Wins == 0));
    }
}

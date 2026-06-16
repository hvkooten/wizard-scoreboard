using System;
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

    [Test]
    public void PauseAndResume_TogglePausedState_ForActiveSession()
    {
        var scoreService = new ScoreService();

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group { Name = "PauseGroup", Players = players };
        var session = scoreService.StartGame(group);

        scoreService.PauseGame(session);
        Assert.IsTrue(session.IsPaused);

        scoreService.ResumeGame(session);
        Assert.IsFalse(session.IsPaused);
    }

    [Test]
    public void StartRound_WhilePaused_ThrowsInvalidOperationException()
    {
        var scoreService = new ScoreService();

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group { Name = "PausedRoundGroup", Players = players };
        var session = scoreService.StartGame(group);
        var bids = session.Players.ToDictionary(p => p.Id, _ => 0);

        scoreService.PauseGame(session);

        Assert.Throws<InvalidOperationException>(() =>
            scoreService.StartRound(session, TrumpSuit.Hearts, bids));
    }

    [Test]
    public void SelectSavedGame_SetsCurrentSession()
    {
        var scoreService = new ScoreService();

        var groupA = new Group
        {
            Name = "A",
            Players = new List<Player>
            {
                new Player { Name = "A1", Order = 0 },
                new Player { Name = "A2", Order = 1 },
                new Player { Name = "A3", Order = 2 }
            }
        };
        var groupB = new Group
        {
            Name = "B",
            Players = new List<Player>
            {
                new Player { Name = "B1", Order = 0 },
                new Player { Name = "B2", Order = 1 },
                new Player { Name = "B3", Order = 2 }
            }
        };

        var sessionA = scoreService.StartGame(groupA);
        var sessionB = scoreService.StartGame(groupB);
        scoreService.PauseGame(sessionA);
        scoreService.PauseGame(sessionB);

        scoreService.SelectSavedGame(sessionA.Id);
        var current = scoreService.GetCurrentSession();

        Assert.NotNull(current);
        Assert.AreEqual(sessionA.Id, current!.Id);
    }
}

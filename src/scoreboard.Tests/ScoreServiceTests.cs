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
    public void UpdateLastRoundActuals_ReplacesScoresWithoutAddingTheRoundTwice()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        var actuals = CreateActuals(session, 0, 1, 0);

        service.UpdateLastRoundActuals(session, actuals);

        Assert.That(session.Players.Select(p => p.CurrentPoints), Is.EqualTo(new[] { -1, -1, 2 }));
    }

    [Test]
    public void UpdateLastRoundActuals_LeavesBidsUnchanged()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        var bids = new Dictionary<Guid, int>(session.Rounds[0].BidByPlayer);

        service.UpdateLastRoundActuals(session, CreateActuals(session, 0, 1, 0));

        Assert.That(session.Rounds[0].BidByPlayer, Is.EquivalentTo(bids));
    }

    [Test]
    public void UpdateLastRoundActuals_LeavesTrumpUnchanged()
    {
        var (service, session) = CreateCompletedRoundForCorrection();

        service.UpdateLastRoundActuals(session, CreateActuals(session, 0, 1, 0));

        Assert.That(session.Rounds[0].Trump, Is.EqualTo(TrumpSuit.Hearts));
    }

    [Test]
    public void UpdateLastRoundActuals_RepeatedCorrectionsDoNotAccumulatePoints()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        service.UpdateLastRoundActuals(session, CreateActuals(session, 0, 1, 0));
        service.UpdateLastRoundActuals(session, CreateActuals(session, 1, 0, 0));

        service.UpdateLastRoundActuals(session, CreateActuals(session, 0, 1, 0));

        Assert.That(session.Players.Select(p => p.CurrentPoints), Is.EqualTo(new[] { -1, -1, 2 }));
    }

    [TestCase(0, 0)]
    [TestCase(10, 10)]
    public void UpdateLastRoundActuals_RestoresHighScoreBeforeCorrectedRound(int previousRecord, int expectedRecord)
    {
        var (service, session) = CreateCompletedRoundForCorrection(previousRecord);

        service.UpdateLastRoundActuals(session, CreateActuals(session, 0, 1, 0));

        Assert.That(session.Players[0].HighestScore, Is.EqualTo(expectedRecord));
    }

    [Test]
    public void UpdateLastRoundActuals_WithoutSnapshotPreservesHistoricalRecord()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        session.Rounds[0].HighestScoreBeforeRoundByPlayer.Clear();

        service.UpdateLastRoundActuals(session, CreateActuals(session, 0, 1, 0));

        Assert.That(session.Players[0].HighestScore, Is.EqualTo(3));
    }

    [Test]
    public void UpdateLastRoundActuals_AfterSerializationRestoresPreRoundRecord()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        var saved = System.Text.Json.JsonSerializer.Serialize(session);
        var restored = System.Text.Json.JsonSerializer.Deserialize<ScoreSession>(saved)
            ?? throw new InvalidOperationException("Session was not deserialized.");

        service.UpdateLastRoundActuals(restored, CreateActuals(restored, 0, 1, 0));

        Assert.That(restored.Players[0].HighestScore, Is.EqualTo(0));
    }

    [Test]
    public void UpdateLastRoundActuals_RecalculatesTotalsIncludingPreviousRounds()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        service.StartRound(session, TrumpSuit.Spades, CreateActuals(session, 0, 0, 0));
        service.FinishRound(session, CreateActuals(session, 2, 0, 0));

        service.UpdateLastRoundActuals(session, CreateActuals(session, 0, 2, 0));

        Assert.That(session.Players.Select(p => p.CurrentPoints), Is.EqualTo(new[] { 5, 0, 4 }));
    }

    [Test]
    public void UpdateLastRoundActuals_LeavesPreviousRoundUnchanged()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        var previousActuals = new Dictionary<Guid, int>(session.Rounds[0].ActualByPlayer);
        service.StartRound(session, TrumpSuit.Spades, CreateActuals(session, 0, 0, 0));
        service.FinishRound(session, CreateActuals(session, 2, 0, 0));

        service.UpdateLastRoundActuals(session, CreateActuals(session, 0, 2, 0));

        Assert.That(session.Rounds[0].ActualByPlayer, Is.EquivalentTo(previousActuals));
    }

    [Test]
    public void UpdateLastRoundActuals_CopiesSubmittedActuals()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        var actuals = CreateActuals(session, 0, 1, 0);

        service.UpdateLastRoundActuals(session, actuals);
        actuals[session.Players[0].Id] = 1;

        Assert.That(session.Rounds[0].ActualByPlayer[session.Players[0].Id], Is.EqualTo(0));
    }

    [TestCase(-1)]
    [TestCase(2)]
    public void UpdateLastRoundActuals_RejectsOutOfRangeTricks(int tricks)
    {
        var (service, session) = CreateCompletedRoundForCorrection();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.UpdateLastRoundActuals(session, CreateActuals(session, tricks, 0, 0)));
    }

    [TestCase(0, 0, 0)]
    [TestCase(1, 1, 0)]
    public void UpdateLastRoundActuals_RejectsIncorrectTotal(int first, int second, int third)
    {
        var (service, session) = CreateCompletedRoundForCorrection();

        Assert.Throws<ArgumentException>(() =>
            service.UpdateLastRoundActuals(session, CreateActuals(session, first, second, third)));
    }

    [Test]
    public void UpdateLastRoundActuals_RejectsMissingPlayer()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        var actuals = CreateActuals(session, 0, 1, 0);
        actuals.Remove(session.Players[2].Id);

        Assert.Throws<ArgumentException>(() => service.UpdateLastRoundActuals(session, actuals));
    }

    [Test]
    public void UpdateLastRoundActuals_RejectsUnknownPlayer()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        var actuals = CreateActuals(session, 0, 1, 0);
        actuals.Remove(session.Players[2].Id);
        actuals[Guid.NewGuid()] = 0;

        Assert.Throws<ArgumentException>(() => service.UpdateLastRoundActuals(session, actuals));
    }

    [Test]
    public void UpdateLastRoundActuals_RejectsPausedGame()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        service.PauseGame(session);

        Assert.Throws<InvalidOperationException>(() =>
            service.UpdateLastRoundActuals(session, CreateActuals(session, 0, 1, 0)));
    }

    [Test]
    public void UpdateLastRoundActuals_RejectsEndedGame()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        service.EndGame(session);

        Assert.Throws<InvalidOperationException>(() =>
            service.UpdateLastRoundActuals(session, CreateActuals(session, 0, 1, 0)));
    }

    [Test]
    public void UpdateLastRoundActuals_RejectsUnfinishedLatestRound()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        service.StartRound(session, TrumpSuit.Spades, CreateActuals(session, 0, 0, 0));

        Assert.Throws<InvalidOperationException>(() =>
            service.UpdateLastRoundActuals(session, CreateActuals(session, 0, 2, 0)));
    }

    [Test]
    public void UpdateLastRoundActuals_RejectsGameWithoutRounds()
    {
        var (service, session) = CreateCompletedRoundForCorrection();
        session.Rounds.Clear();
        session.CurrentRound = 0;

        Assert.Throws<InvalidOperationException>(() =>
            service.UpdateLastRoundActuals(session, CreateActuals(session, 0, 0, 0)));
    }

    [Test]
    public void UpdateLastRoundActuals_RejectsNullSession()
    {
        var service = new ScoreService(new MemoryPreferences());

        Assert.Throws<ArgumentNullException>(() =>
            service.UpdateLastRoundActuals(null!, new Dictionary<Guid, int>()));
    }

    [Test]
    public void UpdateLastRoundActuals_RejectsNullActuals()
    {
        var (service, session) = CreateCompletedRoundForCorrection();

        Assert.Throws<ArgumentNullException>(() => service.UpdateLastRoundActuals(session, null!));
    }

    private static (ScoreService Service, ScoreSession Session) CreateCompletedRoundForCorrection(int highestScore = 0)
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(new Group
        {
            Name = "Correction",
            Players = new List<Player>
            {
                new Player { Name = "A", Order = 0, HighestScore = highestScore },
                new Player { Name = "B", Order = 1, HighestScore = highestScore },
                new Player { Name = "C", Order = 2, HighestScore = highestScore }
            }
        });
        var bids = CreateActuals(session, 1, 0, 0);
        service.StartRound(session, TrumpSuit.Hearts, bids);
        service.FinishRound(session, bids);
        return (service, session);
    }

    private static Dictionary<Guid, int> CreateActuals(ScoreSession session, int first, int second, int third)
    {
        return new Dictionary<Guid, int>
        {
            [session.Players[0].Id] = first,
            [session.Players[1].Id] = second,
            [session.Players[2].Id] = third
        };
    }

    [Test]
    public void StartGame_CreatesSession_WithCorrectMaxRoundsForFourPlayers()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

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
        var scoreService = new ScoreService(new MemoryPreferences());

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
        scoreService.FinishRound(session, CreateActuals(session, 1, 0, 0));

        Assert.AreEqual(1, session.CurrentRound);
        Assert.IsTrue(session.Players.All(p => p.CurrentPoints != 0));
    }

    [Test]
    public void StartGame_UsesGroupBidTotalRuleStartRound_WhenValid()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

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
        var scoreService = new ScoreService(new MemoryPreferences());

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
        var scoreService = new ScoreService(new MemoryPreferences());

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

        Assert.AreEqual(players.Count + 1, session.BidTotalRuleStartRound);
    }

    [Test]
    public void FinishRound_OnFinalRound_EndsGameAndKeepsFinalRoundNumber()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

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
        scoreService.FinishRound(session, CreateActuals(session, 1, 0, 0));

        Assert.AreEqual(1, session.CurrentRound);
        Assert.IsFalse(session.IsActive);
    }

    [Test]
    public void EndGame_EarlyStop_AssignsWinToHighestScorePlayer()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

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
        scoreService.FinishRound(session, CreateActuals(session, 1, 0, 0));

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
        var scoreService = new ScoreService(new MemoryPreferences());

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
        scoreService.FinishRound(session, CreateActuals(session, 1, 0, 0));

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
        var scoreService = new ScoreService(new MemoryPreferences());

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
        var scoreService = new ScoreService(new MemoryPreferences());

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
        var scoreService = new ScoreService(new MemoryPreferences());

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
        var scoreService = new ScoreService(new MemoryPreferences());

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

    [TestCase(3, 0)]
    [TestCase(3, 1)]
    [TestCase(4, 0)]
    [TestCase(4, 1)]
    public void SelectSavedGame_AfterAnotherGameWasRead_ReturnsSelectedSession(int secondPlayerCount, int selectedIndex)
    {
        var (service, firstSession) = CreateCompletedRoundForCorrection();
        service.PauseGame(firstSession);
        var secondSession = service.StartGame(new Group
        {
            Name = "Second saved game",
            Players = Enumerable.Range(0, secondPlayerCount)
                .Select(index => new Player { Name = $"Second {index + 1}", Order = index })
                .ToList()
        });
        service.PauseGame(secondSession);
        var sessions = new[] { firstSession, secondSession };
        service.SelectSavedGame(sessions[1 - selectedIndex].Id);
        _ = service.GetCurrentSession();

        service.SelectSavedGame(sessions[selectedIndex].Id);
        var selected = service.GetCurrentSession();

        Assert.That(selected, Is.SameAs(sessions[selectedIndex]));
    }

    [Test]
    public void EndGame_WithRounds_IncrementsGamesPlayedForAllPlayers()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group { Name = "PlayedGroup", Players = players };
        var session = scoreService.StartGame(group);

        var bids = session.Players.ToDictionary(p => p.Id, _ => 0);
        scoreService.StartRound(session, TrumpSuit.Hearts, bids);
        scoreService.FinishRound(session, CreateActuals(session, 1, 0, 0));

        session.Players[0].CurrentPoints = 8;
        session.Players[1].CurrentPoints = 3;
        session.Players[2].CurrentPoints = 1;

        scoreService.EndGame(session);

        Assert.AreEqual(1, session.Players[0].GamesPlayed);
        Assert.AreEqual(1, session.Players[1].GamesPlayed);
        Assert.AreEqual(1, session.Players[2].GamesPlayed);
        Assert.AreEqual(1, session.Players[0].Wins);
    }

    [Test]
    public void EndGame_WithoutRounds_DoesNotIncrementGamesPlayed()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group { Name = "NoRoundsPlayedGroup", Players = players };
        var session = scoreService.StartGame(group);

        scoreService.EndGame(session);

        Assert.IsTrue(session.Players.All(p => p.GamesPlayed == 0));
    }

    [Test]
    public void GetSavedGames_ReturnsOnlyPausedActiveSessions()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

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

        var saved = scoreService.GetSavedGames().ToList();

        Assert.AreEqual(1, saved.Count);
        Assert.AreEqual(sessionA.Id, saved[0].Id);
        Assert.AreNotEqual(sessionB.Id, saved[0].Id);
    }

    [Test]
    public void SelectSavedGame_IgnoresNonPausedSession()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

        var group = new Group
        {
            Name = "A",
            Players = new List<Player>
            {
                new Player { Name = "A1", Order = 0 },
                new Player { Name = "A2", Order = 1 },
                new Player { Name = "A3", Order = 2 }
            }
        };

        var pausedSession = scoreService.StartGame(group);
        scoreService.PauseGame(pausedSession);

        var activeSession = scoreService.StartGame(group);
        scoreService.SelectSavedGame(activeSession.Id);

        var current = scoreService.GetCurrentSession();
        Assert.NotNull(current);
        Assert.AreEqual(activeSession.Id, current!.Id);
    }

    [Test]
    public void DeleteSavedGame_RemovesPausedSession_FromSavedGames()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

        var group = new Group
        {
            Name = "A",
            Players = new List<Player>
            {
                new Player { Name = "A1", Order = 0 },
                new Player { Name = "A2", Order = 1 },
                new Player { Name = "A3", Order = 2 }
            }
        };

        var session = scoreService.StartGame(group);
        scoreService.PauseGame(session);

        scoreService.DeleteSavedGame(session.Id);

        Assert.IsEmpty(scoreService.GetSavedGames());
    }

    [Test]
    public void DeleteSavedGame_ClearsCurrentSession_WhenDeletingSelectedGame()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

        var group = new Group
        {
            Name = "A",
            Players = new List<Player>
            {
                new Player { Name = "A1", Order = 0 },
                new Player { Name = "A2", Order = 1 },
                new Player { Name = "A3", Order = 2 }
            }
        };

        var session = scoreService.StartGame(group);
        scoreService.PauseGame(session);
        scoreService.SelectSavedGame(session.Id);

        scoreService.DeleteSavedGame(session.Id);

        Assert.IsNull(scoreService.GetCurrentSession());
    }

    [Test]
    public void DeleteSavedGame_DoesNotRemoveActiveNonPausedSession()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

        var group = new Group
        {
            Name = "A",
            Players = new List<Player>
            {
                new Player { Name = "A1", Order = 0 },
                new Player { Name = "A2", Order = 1 },
                new Player { Name = "A3", Order = 2 }
            }
        };

        var pausedSession = scoreService.StartGame(group);
        scoreService.PauseGame(pausedSession);
        var activeSession = scoreService.StartGame(group);

        scoreService.DeleteSavedGame(activeSession.Id);

        Assert.IsNotNull(scoreService.GetCurrentSession());
        Assert.AreEqual(activeSession.Id, scoreService.GetCurrentSession()!.Id);
    }

    [Test]
    public void CancelRound_RemovesStartedRound_AndRestoresRoundCounter()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group { Name = "CancelGroup", Players = players };
        var session = scoreService.StartGame(group);

        var bids = session.Players.ToDictionary(p => p.Id, _ => 1);
        scoreService.StartRound(session, TrumpSuit.Hearts, bids);

        scoreService.CancelRound(session);

        Assert.AreEqual(0, session.CurrentRound);
        Assert.IsEmpty(session.Rounds);
    }

    [Test]
    public void CancelRound_DoesNotChangePlayerScores()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group { Name = "CancelScoreGroup", Players = players };
        var session = scoreService.StartGame(group);

        var bids = session.Players.ToDictionary(p => p.Id, _ => 1);
        scoreService.StartRound(session, TrumpSuit.Hearts, bids);

        scoreService.CancelRound(session);

        Assert.IsTrue(session.Players.All(p => p.CurrentPoints == 0));
    }

    [Test]
    public void CancelRound_RestoresTrumpFromPreviousRound()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group { Name = "CancelTrumpGroup", Players = players };
        var session = scoreService.StartGame(group);

        var bids = session.Players.ToDictionary(p => p.Id, _ => 0);
        scoreService.StartRound(session, TrumpSuit.Hearts, bids);
        scoreService.FinishRound(session, CreateActuals(session, 1, 0, 0));

        var secondBids = session.Players.ToDictionary(p => p.Id, _ => 1);
        scoreService.StartRound(session, TrumpSuit.Spades, secondBids);

        scoreService.CancelRound(session);

        Assert.AreEqual(1, session.CurrentRound);
        Assert.AreEqual(TrumpSuit.Hearts, session.Trump);
    }

    [Test]
    public void CancelRound_WithoutRounds_ThrowsInvalidOperationException()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group { Name = "NoRoundCancelGroup", Players = players };
        var session = scoreService.StartGame(group);

        Assert.Throws<InvalidOperationException>(() => scoreService.CancelRound(session));
    }

    [Test]
    public void CancelRound_AfterRoundFinished_ThrowsInvalidOperationException()
    {
        var scoreService = new ScoreService(new MemoryPreferences());

        var players = new List<Player>
        {
            new Player { Name = "A", Order = 0 },
            new Player { Name = "B", Order = 1 },
            new Player { Name = "C", Order = 2 }
        };

        var group = new Group { Name = "FinishedCancelGroup", Players = players };
        var session = scoreService.StartGame(group);

        var bids = session.Players.ToDictionary(p => p.Id, _ => 0);
        scoreService.StartRound(session, TrumpSuit.Hearts, bids);
        scoreService.FinishRound(session, CreateActuals(session, 1, 0, 0));

        Assert.Throws<InvalidOperationException>(() => scoreService.CancelRound(session));
    }
}

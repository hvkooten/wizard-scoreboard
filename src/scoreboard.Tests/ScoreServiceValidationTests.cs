using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WizardScoreboard.Models;
using WizardScoreboard.Services;

namespace WizardScoreboard.Tests;

public class ScoreServiceValidationTests
{
    [Test]
    public void StartRound_RejectsMissingPlayerBid()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        var bids = TestData.Actuals(session, 1, 0, 0);
        bids.Remove(session.Players[2].Id);

        Assert.Throws<ArgumentException>(() => service.StartRound(session, TrumpSuit.Hearts, bids));
    }

    [Test]
    public void StartRound_RejectsUnknownPlayerBid()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        var bids = TestData.Actuals(session, 1, 0, 0);
        bids.Remove(session.Players[2].Id);
        bids[Guid.NewGuid()] = 0;

        Assert.Throws<ArgumentException>(() => service.StartRound(session, TrumpSuit.Hearts, bids));
    }

    [Test]
    public void StartRound_RejectsUnfinishedPreviousRound()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));

        Assert.Throws<InvalidOperationException>(() =>
            service.StartRound(session, TrumpSuit.Clubs, TestData.Actuals(session, 2, 0, 0)));
    }

    [Test]
    public void StartRound_RejectsUndefinedTrump()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.StartRound(session, (TrumpSuit)99, TestData.Actuals(session, 1, 0, 0)));
    }

    [Test]
    public void FinishRound_RejectsMissingPlayerActual()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));
        var actuals = TestData.Actuals(session, 1, 0, 0);
        actuals.Remove(session.Players[2].Id);

        Assert.Throws<ArgumentException>(() => service.FinishRound(session, actuals));
    }

    [Test]
    public void FinishRound_RejectsUnknownPlayerActual()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));
        var actuals = TestData.Actuals(session, 1, 0, 0);
        actuals.Remove(session.Players[2].Id);
        actuals[Guid.NewGuid()] = 0;

        Assert.Throws<ArgumentException>(() => service.FinishRound(session, actuals));
    }

    [TestCase(0, 0, 0)]
    [TestCase(1, 1, 0)]
    public void FinishRound_RejectsIncorrectTotal(int first, int second, int third)
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));

        Assert.Throws<ArgumentException>(() => service.FinishRound(session, TestData.Actuals(session, first, second, third)));
    }

    [Test]
    public void FinishRound_RejectsRepeatedCompletion()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));
        service.FinishRound(session, TestData.Actuals(session, 1, 0, 0));

        Assert.Throws<InvalidOperationException>(() => service.FinishRound(session, TestData.Actuals(session, 1, 0, 0)));
    }

    [Test]
    public void FinishRound_RejectsIncompleteBids()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        var round = service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));
        round.BidByPlayer.Remove(session.Players[2].Id);

        Assert.Throws<InvalidOperationException>(() => service.FinishRound(session, TestData.Actuals(session, 1, 0, 0)));
    }

    [Test]
    public void PauseGame_DiscardsUnfinishedRound()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));

        service.PauseGame(session);

        Assert.That((session.Rounds.Count, session.CurrentRound, session.Trump), Is.EqualTo((0, 0, TrumpSuit.None)));
    }

    [Test]
    public void ResumeGame_AfterIncompleteRoundReplaysSameRound()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));
        service.PauseGame(session);
        service.ResumeGame(session);

        var round = service.StartRound(session, TrumpSuit.Spades, TestData.Actuals(session, 0, 1, 0));

        Assert.That((round.RoundNumber, round.DealerPlayerId), Is.EqualTo((1, session.Players[0].Id)));
    }

    [Test]
    public void EndGame_WithOnlyUnfinishedRoundDoesNotAwardStatistics()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));

        service.EndGame(session);

        Assert.That(session.Players.Select(p => (p.Wins, p.GamesPlayed)), Is.EqualTo(new[] { (0, 0), (0, 0), (0, 0) }));
    }

    [Test]
    public void Constructor_NullPreferencesThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ScoreService(null!));
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(7)]
    public void StartGame_RejectsUnsupportedPlayerCount(int count)
    {
        var service = new ScoreService(new MemoryPreferences());

        Assert.Throws<ArgumentException>(() => service.StartGame(TestData.CreateGroup(count)));
    }

    [TestCase(3, 20)]
    [TestCase(4, 15)]
    [TestCase(5, 12)]
    [TestCase(6, 10)]
    public void StartGame_UsesCorrectRoundLimit(int count, int expected)
    {
        var service = new ScoreService(new MemoryPreferences());

        Assert.That(service.StartGame(TestData.CreateGroup(count)).MaxRounds, Is.EqualTo(expected));
    }

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(20, 20)]
    [TestCase(Group.PlayerCountRule, 4)]
    [TestCase(Group.DoublePlayerCountRule, 7)]
    [TestCase(-99, 4)]
    [TestCase(21, 4)]
    public void StartGame_ResolvesBidRule(int rule, int expected)
    {
        var group = TestData.CreateGroup();
        group.BidTotalRuleStartRound = rule;
        var service = new ScoreService(new MemoryPreferences());

        Assert.That(service.StartGame(group).BidTotalRuleStartRound, Is.EqualTo(expected));
    }

    [Test]
    public void StartGame_OrdersPlayersWithoutReorderingTheGroup()
    {
        var group = TestData.CreateGroup();
        group.Players.Reverse();
        var service = new ScoreService(new MemoryPreferences());

        var session = service.StartGame(group);

        Assert.That(session.Players.Select(p => p.Order), Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void StartGame_CopiesPlayersWithoutChangingOriginalPoints()
    {
        var group = TestData.CreateGroup();
        group.Players[0].CurrentPoints = 42;
        var session = new ScoreService(new MemoryPreferences()).StartGame(group);

        session.Players[0].CurrentPoints = 9;

        Assert.That(group.Players[0].CurrentPoints, Is.EqualTo(42));
    }

    [Test]
    public void StartGame_ResetsGamePointsWhileRetainingLifetimeStatistics()
    {
        var group = TestData.CreateGroup();
        group.Players[0].CurrentPoints = 42;
        group.Players[0].HighestScore = 50;
        group.Players[0].Wins = 3;
        group.Players[0].GamesPlayed = 7;

        var player = new ScoreService(new MemoryPreferences()).StartGame(group).Players[0];

        Assert.That((player.CurrentPoints, player.HighestScore, player.Wins, player.GamesPlayed), Is.EqualTo((0, 50, 3, 7)));
    }

    [Test]
    public void PauseGame_RejectsEndedSession()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.EndGame(session);

        Assert.Throws<InvalidOperationException>(() => service.PauseGame(session));
    }

    [Test]
    public void ResumeGame_RejectsEndedSession()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.EndGame(session);

        Assert.Throws<InvalidOperationException>(() => service.ResumeGame(session));
    }

    [Test]
    public void StartRound_RejectsEndedSession()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.EndGame(session);

        Assert.Throws<InvalidOperationException>(() => service.StartRound(session, TrumpSuit.Clubs, TestData.Actuals(session, 0, 0, 0)));
    }

    [Test]
    public void StartRound_RejectsExhaustedRoundLimit()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        session.CurrentRound = session.MaxRounds;

        Assert.Throws<InvalidOperationException>(() => service.StartRound(session, TrumpSuit.Clubs, TestData.Actuals(session, 0, 0, 0)));
    }

    [TestCase(-1)]
    [TestCase(2)]
    public void StartRound_RejectsOutOfRangeBid(int bid)
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());

        Assert.Throws<ArgumentOutOfRangeException>(() => service.StartRound(session, TrumpSuit.Clubs, TestData.Actuals(session, bid, 0, 0)));
    }

    [Test]
    public void StartRound_CopiesSubmittedBids()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        var bids = TestData.Actuals(session, 1, 0, 0);
        var round = service.StartRound(session, TrumpSuit.Clubs, bids);

        bids[session.Players[0].Id] = 0;

        Assert.That(round.BidByPlayer[session.Players[0].Id], Is.EqualTo(1));
    }

    [Test]
    public void FinishRound_WithoutRoundThrows()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());

        Assert.Throws<InvalidOperationException>(() => service.FinishRound(session, TestData.Actuals(session, 0, 0, 0)));
    }

    [TestCase(-1)]
    [TestCase(2)]
    public void FinishRound_RejectsOutOfRangeActuals(int actual)
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Clubs, TestData.Actuals(session, 1, 0, 0));

        Assert.Throws<ArgumentOutOfRangeException>(() => service.FinishRound(session, TestData.Actuals(session, actual, 0, 0)));
    }

    [Test]
    public void EndGame_RepeatedCallDoesNotAwardStatisticsTwice()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Clubs, TestData.Actuals(session, 1, 0, 0));
        service.FinishRound(session, TestData.Actuals(session, 1, 0, 0));
        service.EndGame(session);

        service.EndGame(session);

        Assert.That(session.Players.Select(p => (p.Wins, p.GamesPlayed)), Is.EqualTo(new[] { (1, 1), (0, 1), (0, 1) }));
    }

    [Test]
    public void GetActiveSessions_ExcludesEndedButIncludesPausedGames()
    {
        var service = new ScoreService(new MemoryPreferences());
        var ended = service.StartGame(TestData.CreateGroup());
        var paused = service.StartGame(TestData.CreateGroup());
        var running = service.StartGame(TestData.CreateGroup());
        service.EndGame(ended);
        service.PauseGame(paused);

        Assert.That(service.GetActiveSessions().Select(s => s.Id), Is.EquivalentTo(new[] { paused.Id, running.Id }));
    }

    [Test]
    public void GetCurrentSession_WhenSelectedGameEndsFallsBackToNewestActiveGame()
    {
        var service = new ScoreService(new MemoryPreferences());
        var older = service.StartGame(TestData.CreateGroup());
        older.StartDate = new DateTime(2026, 1, 1);
        var newer = service.StartGame(TestData.CreateGroup());
        newer.StartDate = older.StartDate.AddDays(1);
        var selected = service.StartGame(TestData.CreateGroup());

        service.EndGame(selected);

        Assert.That(service.GetCurrentSession(), Is.SameAs(newer));
    }

    [Test]
    public void SelectSavedGame_UnknownIdPreservesSelection()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());

        service.SelectSavedGame(Guid.NewGuid());

        Assert.That(service.GetCurrentSession(), Is.SameAs(session));
    }

    [Test]
    public void DeleteSavedGame_UnknownIdPreservesGames()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.PauseGame(session);

        service.DeleteSavedGame(Guid.NewGuid());

        Assert.That(service.GetSavedGames().Select(s => s.Id), Is.EqualTo(new[] { session.Id }));
    }

    [Test]
    public void CancelRound_NullSessionThrows()
    {
        var service = new ScoreService(new MemoryPreferences());

        Assert.Throws<ArgumentNullException>(() => service.CancelRound(null!));
    }
}

using System;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using WizardScoreboard.Models;
using WizardScoreboard.Services;

namespace WizardScoreboard.Tests;

public class ScoreServicePersistenceTests
{
    [Test]
    public void StartRound_DoesNotPersistIncompleteRound()
    {
        var preferences = new MemoryPreferences();
        var service = new ScoreService(preferences);
        var session = service.StartGame(TestData.CreateGroup());

        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));

        var stored = JsonSerializer.Deserialize<ScoreSession[]>(preferences.Get("paused_sessions_storage_v1", ""));
        Assert.That(stored?.Single().Rounds, Is.Empty);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("{broken")]
    [TestCase("{}")]
    public void NewService_EmptyOrInvalidStoredDataHasNoCurrentGame(string raw)
    {
        var preferences = new MemoryPreferences();
        preferences.Set("paused_sessions_storage_v1", raw);

        Assert.That(new ScoreService(preferences).GetCurrentSession(), Is.Null);
    }

    [Test]
    public void NewService_RestoresRunningGameAsPaused()
    {
        var preferences = new MemoryPreferences();
        new ScoreService(preferences).StartGame(TestData.CreateGroup());

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(restored.IsPaused, Is.True);
    }

    [Test]
    public void NewService_IgnoresInactiveSessionsInStoredData()
    {
        var preferences = new MemoryPreferences();
        var active = new ScoreSession { IsActive = true };
        var ended = new ScoreSession { IsActive = false };
        preferences.Set("paused_sessions_storage_v1", JsonSerializer.Serialize(new[] { active, ended }));

        var restored = new ScoreService(preferences).GetActiveSessions();

        Assert.That(restored.Select(s => s.Id), Is.EqualTo(new[] { active.Id }));
    }

    [Test]
    public void NewService_SelectsNewestSavedGame()
    {
        var preferences = new MemoryPreferences();
        var service = new ScoreService(preferences);
        var newer = service.StartGame(TestData.CreateGroup());
        newer.StartDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var older = service.StartGame(TestData.CreateGroup());
        older.StartDate = newer.StartDate.AddDays(-1);
        service.PauseGame(older);

        var restored = new ScoreService(preferences).GetCurrentSession();

        Assert.That(restored?.Id, Is.EqualTo(newer.Id));
    }

    [Test]
    public void NewService_RestoresCorrectedRoundWithoutChangingBids()
    {
        var preferences = new MemoryPreferences();
        var service = new ScoreService(preferences);
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));
        service.FinishRound(session, TestData.Actuals(session, 1, 0, 0));
        service.UpdateLastRoundActuals(session, TestData.Actuals(session, 0, 1, 0));
        var expected = JsonSerializer.Serialize(session.Rounds.Single());

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(JsonSerializer.Serialize(restored.Rounds.Single()), Is.EqualTo(expected));
    }

    [Test]
    public void NewService_RestoresCorrectedScores()
    {
        var preferences = new MemoryPreferences();
        var service = new ScoreService(preferences);
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));
        service.FinishRound(session, TestData.Actuals(session, 1, 0, 0));
        service.UpdateLastRoundActuals(session, TestData.Actuals(session, 0, 1, 0));

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(restored.Players.Select(p => p.CurrentPoints), Is.EqualTo(new[] { -1, -1, 2 }));
    }

    [Test]
    public void UpdateLastRoundActuals_AfterReloadRestoresPreRoundHighScore()
    {
        var preferences = new MemoryPreferences();
        var service = new ScoreService(preferences);
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));
        service.FinishRound(session, TestData.Actuals(session, 1, 0, 0));
        var reloaded = new ScoreService(preferences);
        var restored = reloaded.GetSavedGames().Single();
        reloaded.ResumeGame(restored);

        reloaded.UpdateLastRoundActuals(restored, TestData.Actuals(restored, 0, 1, 0));

        Assert.That(restored.Players[0].HighestScore, Is.EqualTo(0));
    }

    [Test]
    public void DeleteSavedGame_PersistsRemoval()
    {
        var preferences = new MemoryPreferences();
        var service = new ScoreService(preferences);
        var session = service.StartGame(TestData.CreateGroup());
        service.PauseGame(session);

        service.DeleteSavedGame(session.Id);

        Assert.That(new ScoreService(preferences).GetActiveSessions(), Is.Empty);
    }

    [Test]
    public void EndGame_RemovesGameFromPersistentStorage()
    {
        var preferences = new MemoryPreferences();
        var service = new ScoreService(preferences);
        var session = service.StartGame(TestData.CreateGroup());

        service.EndGame(session);

        Assert.That(new ScoreService(preferences).GetCurrentSession(), Is.Null);
    }

    [Test]
    public void CancelRound_PersistsPreviousRoundState()
    {
        var preferences = new MemoryPreferences();
        var service = new ScoreService(preferences);
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Clubs, TestData.Actuals(session, 0, 0, 0));

        service.CancelRound(session);

        var restored = new ScoreService(preferences).GetSavedGames().Single();
        Assert.That((restored.CurrentRound, restored.Rounds.Count, restored.Trump), Is.EqualTo((0, 0, TrumpSuit.None)));
    }

    [Test]
    public void NewService_ResumeGameRemovesSessionFromSavedList()
    {
        var preferences = new MemoryPreferences();
        new ScoreService(preferences).StartGame(TestData.CreateGroup());
        var service = new ScoreService(preferences);
        var restored = service.GetSavedGames().Single();

        service.ResumeGame(restored);

        Assert.That(service.GetSavedGames(), Is.Empty);
    }

    [Test]
    public void NewService_RestoresCompleteRoundAndPlayerData()
    {
        var preferences = new MemoryPreferences();
        var service = new ScoreService(preferences);
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Spades, TestData.Actuals(session, 1, 0, 0));
        service.FinishRound(session, TestData.Actuals(session, 1, 0, 0));
        service.PauseGame(session);
        var expected = JsonSerializer.Serialize(session);

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(JsonSerializer.Serialize(restored), Is.EqualTo(expected));
    }

    [Test]
    public void StartRound_LeavesLiveInputIntactWhenAnotherGameIsSaved()
    {
        var service = new ScoreService(new MemoryPreferences());
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));
        var expected = JsonSerializer.Serialize(session);

        service.StartGame(TestData.CreateGroup());

        Assert.That(JsonSerializer.Serialize(session), Is.EqualTo(expected));
    }

    [Test]
    public void StartRound_PersistsOnlyPreviousCompletedRoundState()
    {
        var preferences = new MemoryPreferences();
        var service = new ScoreService(preferences);
        var session = service.StartGame(TestData.CreateGroup());
        service.StartRound(session, TrumpSuit.Hearts, TestData.Actuals(session, 1, 0, 0));
        service.FinishRound(session, TestData.Actuals(session, 1, 0, 0));
        var expected = preferences.Get("paused_sessions_storage_v1", "");

        service.StartRound(session, TrumpSuit.Spades, TestData.Actuals(session, 2, 0, 0));

        Assert.That(preferences.Get("paused_sessions_storage_v1", ""), Is.EqualTo(expected));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void NewService_DiscardsFirstIncompleteRoundAndAllLaterRounds(int incompleteIndex)
    {
        var preferences = CreateLegacySave(session => session.Rounds[incompleteIndex].ActualByPlayer.Clear());

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(restored.Rounds.Select(r => r.RoundNumber), Is.EqualTo(Enumerable.Range(1, incompleteIndex)));
    }

    [Test]
    public void NewService_DiscardsRoundWithMissingBid()
    {
        var preferences = CreateLegacySave(session => session.Rounds[1].BidByPlayer.Remove(session.Players[2].Id));

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(restored.Rounds.Count, Is.EqualTo(1));
    }

    [Test]
    public void NewService_DiscardsRoundWithMissingActual()
    {
        var preferences = CreateLegacySave(session => session.Rounds[1].ActualByPlayer.Remove(session.Players[2].Id));

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(restored.Rounds.Count, Is.EqualTo(1));
    }

    [Test]
    public void NewService_DiscardsRoundWithUnknownPlayer()
    {
        var preferences = CreateLegacySave(session =>
        {
            session.Rounds[1].ActualByPlayer.Remove(session.Players[2].Id);
            session.Rounds[1].ActualByPlayer[Guid.NewGuid()] = 0;
        });

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(restored.Rounds.Count, Is.EqualTo(1));
    }

    [TestCase(-1)]
    [TestCase(3)]
    public void NewService_DiscardsRoundWithInvalidBid(int bid)
    {
        var preferences = CreateLegacySave(session => session.Rounds[1].BidByPlayer[session.Players[0].Id] = bid);

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(restored.Rounds.Count, Is.EqualTo(1));
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(3)]
    public void NewService_DiscardsRoundWithInvalidActuals(int actual)
    {
        var preferences = CreateLegacySave(session => session.Rounds[1].ActualByPlayer[session.Players[0].Id] = actual);

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(restored.Rounds.Count, Is.EqualTo(1));
    }

    [Test]
    public void NewService_DiscardsRoundsAfterNumberingGap()
    {
        var preferences = CreateLegacySave(session => session.Rounds.RemoveAt(1));

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(restored.Rounds.Select(r => r.RoundNumber), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void NewService_RepairRestoresRoundTrumpAndDealer()
    {
        var preferences = CreateLegacySave(session => session.Rounds[1].ActualByPlayer.Clear());

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That((restored.CurrentRound, restored.Trump, restored.CurrentDealer),
            Is.EqualTo((1, TrumpSuit.Hearts, restored.Players[1].Id)));
    }

    [Test]
    public void NewService_RepairRecalculatesScoresWithoutDiscardedRounds()
    {
        var preferences = CreateLegacySave(session => session.Rounds[1].ActualByPlayer.Clear());

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(restored.Players.Select(p => p.CurrentPoints), Is.EqualTo(new[] { 3, 2, 2 }));
    }

    [Test]
    public void NewService_RepairRestoresHighScoresBeforeDiscardedRounds()
    {
        var preferences = CreateLegacySave(session => session.Rounds[1].ActualByPlayer.Clear());

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(restored.Players.Select(p => p.HighestScore), Is.EqualTo(new[] { 3, 2, 2 }));
    }

    [Test]
    public void NewService_RepairWithoutSnapshotPreservesHistoricalRecords()
    {
        var preferences = CreateLegacySave(session =>
        {
            session.Rounds[1].ActualByPlayer.Clear();
            session.Rounds[1].HighestScoreBeforeRoundByPlayer.Clear();
        });

        var restored = new ScoreService(preferences).GetSavedGames().Single();

        Assert.That(restored.Players.Select(p => p.HighestScore), Is.EqualTo(new[] { 12, 6, 6 }));
    }

    [Test]
    public void NewService_RepairRemovesIncompleteRoundsFromStorage()
    {
        var preferences = CreateLegacySave(session => session.Rounds[1].ActualByPlayer.Clear());

        _ = new ScoreService(preferences);

        var stored = JsonSerializer.Deserialize<ScoreSession[]>(preferences.Get("paused_sessions_storage_v1", ""));
        Assert.That(stored?.Single().Rounds.Select(r => r.RoundNumber), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void ResumeGame_ReplaysFirstDiscardedRound()
    {
        var preferences = CreateLegacySave(session => session.Rounds[1].ActualByPlayer.Clear());
        var service = new ScoreService(preferences);
        var restored = service.GetSavedGames().Single();
        service.ResumeGame(restored);

        var replayed = service.StartRound(restored, TrumpSuit.Clubs, TestData.Actuals(restored, 2, 0, 0));

        Assert.That((replayed.RoundNumber, replayed.DealerPlayerId), Is.EqualTo((2, restored.Players[1].Id)));
    }

    private static MemoryPreferences CreateLegacySave(Action<ScoreSession> corrupt)
    {
        var preferences = new MemoryPreferences();
        var service = new ScoreService(preferences);
        var session = service.StartGame(TestData.CreateGroup());
        for (var number = 1; number <= 3; number++)
        {
            service.StartRound(session, (TrumpSuit)number, TestData.Actuals(session, number, 0, 0));
            service.FinishRound(session, TestData.Actuals(session, number, 0, 0));
        }

        corrupt(session);
        preferences.Set("paused_sessions_storage_v1", JsonSerializer.Serialize(new[] { session }));
        return preferences;
    }
}

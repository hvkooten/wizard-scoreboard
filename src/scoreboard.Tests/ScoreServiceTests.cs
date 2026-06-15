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
        var groupService = new GroupService();
        var scoreService = new ScoreService();

        var players = new List<Player>
        {
            new Player { Name = "A", Order=0 },
            new Player { Name = "B", Order=1 },
            new Player { Name = "C", Order=2 },
            new Player { Name = "D", Order=3 }
        };

        var group = groupService.CreateGroup("Test", players);
        var session = scoreService.StartGame(group);

        Assert.NotNull(session);
        Assert.AreEqual(15, session.MaxRounds);
        Assert.IsTrue(session.IsActive);
    }

    [Test]
    public void StartRound_AndFinish_UpdatesPlayerScores()
    {
        var groupService = new GroupService();
        var scoreService = new ScoreService();

        var players = new List<Player>
        {
            new Player { Name = "A", Order=0 },
            new Player { Name = "B", Order=1 },
            new Player { Name = "C", Order=2 }
        };

        var group = groupService.CreateGroup("Test", players);
        var session = scoreService.StartGame(group);

        var bids = session.Players.ToDictionary(p => p.Id, p => 1);

        var round = scoreService.StartRound(session, TrumpSuit.Hearts, bids);
        scoreService.FinishRound(session, bids);

        Assert.AreEqual(1, session.CurrentRound);
        Assert.IsTrue(session.Players.All(p => p.CurrentPoints != 0));
    }
}

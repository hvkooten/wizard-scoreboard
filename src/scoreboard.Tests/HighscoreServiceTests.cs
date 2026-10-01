using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WizardScoreboard.Models;
using WizardScoreboard.Services;

namespace WizardScoreboard.Tests;

public class HighscoreServiceTests
{
    [Test]
    public void UpdateHighscores_EmptyInputClearsPreviousResults()
    {
        var service = new HighscoreService();
        service.UpdateHighscores(new[] { TestData.CreateGroup() });

        service.UpdateHighscores(System.Array.Empty<Group>());

        Assert.That(service.GetHighscores(), Is.Empty);
    }

    [Test]
    public void UpdateHighscores_RepeatedUpdateDoesNotDoubleStatistics()
    {
        var service = new HighscoreService();
        var group = TestData.CreateGroup();
        group.Players[0].Wins = 2;
        group.Players[0].GamesPlayed = 5;
        group.Players[0].HighestScore = 30;
        service.UpdateHighscores(new[] { group });

        service.UpdateHighscores(new[] { group });

        var player = service.GetHighscores().Single(p => p.Id == group.Players[0].Id);
        Assert.That((player.Wins, player.GamesPlayed, player.HighestScore), Is.EqualTo((2, 5, 30)));
    }

    [Test]
    public void UpdateHighscores_NewInputReplacesOldPlayers()
    {
        var service = new HighscoreService();
        service.UpdateHighscores(new[] { TestData.CreateGroup() });
        var group = new Group { Players = new List<Player> { new Player { Name = "Replacement" } } };

        service.UpdateHighscores(new[] { group });

        Assert.That(service.GetHighscores().Select(p => p.Name), Is.EqualTo(new[] { "Replacement" }));
    }

    [Test]
    public void UpdateHighscores_DoesNotExposeOriginalPlayerObjects()
    {
        var service = new HighscoreService();
        var group = TestData.CreateGroup();
        service.UpdateHighscores(new[] { group });

        service.GetHighscores().First().Wins = 99;

        Assert.That(group.Players.Select(p => p.Wins), Is.EqualTo(new[] { 0, 0, 0 }));
    }

    [Test]
    public void UpdateHighscores_MergesSameNamesAcrossGroups()
    {
        var service = new HighscoreService();

        var groups = new List<Group>
        {
            new Group
            {
                Name = "G1",
                Players = new List<Player>
                {
                    new Player { Name = "Alex", Wins = 2, GamesPlayed = 3, HighestScore = 18 },
                    new Player { Name = "Bo", Wins = 1, GamesPlayed = 2, HighestScore = 12 }
                }
            },
            new Group
            {
                Name = "G2",
                Players = new List<Player>
                {
                    new Player { Name = " alex ", Wins = 4, GamesPlayed = 5, HighestScore = 25 }
                }
            }
        };

        service.UpdateHighscores(groups);

        var alex = service.GetHighscores().First(p => p.Name.Trim().Equals("Alex", System.StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(6, alex.Wins);
        Assert.AreEqual(8, alex.GamesPlayed);
        Assert.AreEqual(25, alex.HighestScore);
    }

    [Test]
    public void GetHighscores_OrdersByWinsBestPlayed()
    {
        var service = new HighscoreService();

        var groups = new List<Group>
        {
            new Group
            {
                Name = "G1",
                Players = new List<Player>
                {
                    new Player { Name = "A", Wins = 3, GamesPlayed = 4, HighestScore = 20 },
                    new Player { Name = "B", Wins = 3, GamesPlayed = 7, HighestScore = 20 },
                    new Player { Name = "C", Wins = 3, GamesPlayed = 3, HighestScore = 25 },
                    new Player { Name = "D", Wins = 2, GamesPlayed = 10, HighestScore = 99 }
                }
            }
        };

        service.UpdateHighscores(groups);

        var ordered = service.GetHighscores().Select(p => p.Name).ToList();
        CollectionAssert.AreEqual(new[] { "C", "B", "A", "D" }, ordered);
    }
}

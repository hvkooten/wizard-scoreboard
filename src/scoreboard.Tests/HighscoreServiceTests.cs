using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WizardScoreboard.Models;
using WizardScoreboard.Services;

namespace WizardScoreboard.Tests;

public class HighscoreServiceTests
{
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

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
}

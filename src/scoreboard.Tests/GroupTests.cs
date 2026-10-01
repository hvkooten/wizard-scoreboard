using NUnit.Framework;
using WizardScoreboard.Models;

namespace WizardScoreboard.Tests;

public class GroupTests
{
    [TestCase(3, false, 4)]
    [TestCase(4, false, 5)]
    [TestCase(5, false, 6)]
    [TestCase(6, false, 7)]
    [TestCase(3, true, 7)]
    [TestCase(4, true, 9)]
    [TestCase(5, true, 11)]
    [TestCase(6, true, 13)]
    public void PlayerCountStartRound_StartsAfterTheRequiredPlayerCount(int players, bool doubleCount, int expected)
    {
        Assert.That(Group.PlayerCountStartRound(players, doubleCount), Is.EqualTo(expected));
    }

    [TestCase(3, 20)]
    [TestCase(4, 15)]
    [TestCase(5, 12)]
    [TestCase(6, 10)]
    [TestCase(0, 20)]
    [TestCase(-1, 20)]
    [TestCase(7, 10)]
    public void MaxRounds_UsesDeckSizeAndClampsPlayerCount(int players, int expected)
    {
        Assert.That(Group.MaxRounds(players), Is.EqualTo(expected));
    }
}

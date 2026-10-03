using NUnit.Framework;
using WizardScoreboard.Models;
using WizardScoreboard.Services;

namespace WizardScoreboard.Tests;

public class ScoreServiceLastRoundTrumpTests
{
    [TestCase(false, 19, true)]
    [TestCase(false, 18, false)]
    [TestCase(true, 19, false)]
    public void IsNextRoundWithoutTrump_DependsOnLastRoundAndDealerPicksTrump(bool dealerPicksTrump, int currentRound, bool expected)
    {
        var session = new ScoreSession { AllowNoTrump = dealerPicksTrump, MaxRounds = 20, CurrentRound = currentRound };

        Assert.That(ScoreService.IsNextRoundWithoutTrump(session), Is.EqualTo(expected));
    }
}

using System.Globalization;
using NUnit.Framework;
using WizardScoreboard.Resources;

namespace WizardScoreboard.Tests;

public class LocalizationTests
{
    [TestCase("en-US", "Alex (Dealer, bid: 3)")]
    [TestCase("nl-NL", "Alex (Deler, bod: 3)")]
    [TestCase("de-DE", "Alex (Geber, Gebot: 3)")]
    [TestCase("es-ES", "Alex (Repartidor, apuesta: 3)")]
    [TestCase("fr-FR", "Alex (Donneur, annonce : 3)")]
    public void DealerBidLabelTemplate_IncludesPlayerNameDealerAndBid(string cultureName, string expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            var label = string.Format(Localization.GetString("DealerBidLabelTemplate"),
                "Alex", Localization.GetString("DealerLabel"), 3);

            Assert.That(label, Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US", "Alex (bid: 3)")]
    [TestCase("nl-NL", "Alex (bod: 3)")]
    [TestCase("de-DE", "Alex (Gebot: 3)")]
    [TestCase("es-ES", "Alex (apuesta: 3)")]
    [TestCase("fr-FR", "Alex (annonce : 3)")]
    public void PlayerBidLabelTemplate_IncludesPlayerNameAndBid(string cultureName, string expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            var label = string.Format(Localization.GetString("PlayerBidLabelTemplate"), "Alex", 3);

            Assert.That(label, Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}

using System.Globalization;
using NUnit.Framework;
using WizardScoreboard.Resources;

namespace WizardScoreboard.Tests;

[NonParallelizable]
public class LocalizationTests
{
    [TestCase("nl-NL", "nl-NL")]
    [TestCase("EN-us", "en-US")]
    [TestCase("de-DE", "de-DE")]
    [TestCase("fr-FR", "fr-FR")]
    [TestCase("es-ES", "es-ES")]
    [TestCase("nl", "nl-NL")]
    [TestCase("nl-BE", "nl-NL")]
    [TestCase("en-GB", "en-US")]
    [TestCase("fr-CA", "fr-FR")]
    [TestCase("de-AT", "de-DE")]
    [TestCase("es-MX", "es-ES")]
    [TestCase("ja-JP", "en-US")]
    [TestCase("invalid/culture", "en-US")]
    [TestCase(null, "en-US")]
    [TestCase("", "en-US")]
    [TestCase("   ", "en-US")]
    public void ResolveSupportedCulture_ReturnsSupportedCultureOrEnglish(string? input, string expected)
    {
        Assert.That(Localization.ResolveSupportedCulture(input), Is.EqualTo(expected));
    }

    [Test]
    public void GetString_UnknownKey_ReturnsTheKey()
    {
        Assert.That(Localization.GetString("MissingResourceForUnitTest"), Is.EqualTo("MissingResourceForUnitTest"));
    }

    [TestCase("nl-BE", "nl-NL")]
    [TestCase("invalid/culture", "en-US")]
    public void SetCulture_UpdatesBothDefaultCultures(string input, string expected)
    {
        var previousCulture = CultureInfo.DefaultThreadCurrentCulture;
        var previousUiCulture = CultureInfo.DefaultThreadCurrentUICulture;
        try
        {
            var resolved = Localization.SetCulture(input);

            Assert.That((resolved, CultureInfo.DefaultThreadCurrentCulture?.Name, CultureInfo.DefaultThreadCurrentUICulture?.Name),
                Is.EqualTo((expected, expected, expected)));
        }
        finally
        {
            CultureInfo.DefaultThreadCurrentCulture = previousCulture;
            CultureInfo.DefaultThreadCurrentUICulture = previousUiCulture;
        }
    }

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

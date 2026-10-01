using System;
using System.Globalization;
using NUnit.Framework;
using WizardScoreboard.Services;

namespace WizardScoreboard.Tests;

[NonParallelizable]
public class TrumpPaletteServiceTests
{
    [Test]
    public void Constructor_NullPreferencesThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new TrumpPaletteService(null!));
    }

    [Test]
    public void GetMode_WithoutPreferenceDefaultsToCardSuits()
    {
        Assert.That(new TrumpPaletteService(new MemoryPreferences()).GetMode(), Is.EqualTo(TrumpPaletteMode.CardSuits));
    }

    [TestCase("")]
    [TestCase("UnknownPalette")]
    [TestCase("   ")]
    public void GetMode_UnrecognizedPreferenceFallsBackToCardSuits(string stored)
    {
        var preferences = new MemoryPreferences();
        preferences.Set("trump_palette_mode", stored);

        Assert.That(new TrumpPaletteService(preferences).GetMode(), Is.EqualTo(TrumpPaletteMode.CardSuits));
    }

    [TestCase(TrumpPaletteMode.CardSuits)]
    [TestCase(TrumpPaletteMode.FourColors)]
    public void SetMode_PersistsAcrossServiceInstances(TrumpPaletteMode mode)
    {
        var preferences = new MemoryPreferences();

        new TrumpPaletteService(preferences).SetMode(mode);

        Assert.That(new TrumpPaletteService(preferences).GetMode(), Is.EqualTo(mode));
    }

    [TestCase(TrumpPaletteMode.CardSuits, "CardSuits")]
    [TestCase(TrumpPaletteMode.FourColors, "FourColors")]
    public void SetMode_UsesExistingStringStorageFormat(TrumpPaletteMode mode, string expected)
    {
        var preferences = new MemoryPreferences();

        new TrumpPaletteService(preferences).SetMode(mode);

        Assert.That(preferences.Get("trump_palette_mode", ""), Is.EqualTo(expected));
    }

    [TestCase("en-US", TrumpPaletteMode.CardSuits, new[] { "None", "Hearts", "Diamonds", "Clubs", "Spades" })]
    [TestCase("en-US", TrumpPaletteMode.FourColors, new[] { "None", "Red", "Yellow", "Green", "Blue" })]
    [TestCase("nl-NL", TrumpPaletteMode.CardSuits, new[] { "Geen", "Harten", "Ruiten", "Klaveren", "Schoppen" })]
    [TestCase("nl-NL", TrumpPaletteMode.FourColors, new[] { "Geen", "Rood", "Geel", "Groen", "Blauw" })]
    public void GetTrumpLabels_UsesSelectedModeAndCulture(string cultureName, TrumpPaletteMode mode, string[] expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var service = new TrumpPaletteService(new MemoryPreferences());
            service.SetMode(mode);

            Assert.That(service.GetTrumpLabels(), Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}

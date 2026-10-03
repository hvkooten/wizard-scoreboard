using NUnit.Framework;
using WizardScoreboard.Pages;
using WizardScoreboard.Services;

namespace WizardScoreboard.Tests;

public class RulesPageTests
{
    private const string Html = "<html><head></head><body></body></html>";

    [TestCase(TrumpPaletteMode.CardSuits, ".suit-colors{display:none !important;}")]
    [TestCase(TrumpPaletteMode.FourColors, ".suit-cards{display:none !important;}")]
    public void ApplySuitStyle_HidesVariantNotMatchingPalette(TrumpPaletteMode mode, string expectedCss)
    {
        var result = RulesPage.ApplySuitStyle(Html, mode);

        Assert.That(result, Does.Contain(expectedCss + "</style></head>"));
    }
}

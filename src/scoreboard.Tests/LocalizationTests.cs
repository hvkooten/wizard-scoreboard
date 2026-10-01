using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using WizardScoreboard.Resources;

namespace WizardScoreboard.Tests;

[NonParallelizable]
public class LocalizationTests
{
    [TestCase("en-US", "Save group")]
    [TestCase("nl-NL", "Groep opslaan")]
    [TestCase("de-DE", "Gruppe speichern")]
    [TestCase("es-ES", "Guardar grupo")]
    [TestCase("fr-FR", "Enregistrer le groupe")]
    public void SaveGroup_UsesSelectedLanguage(string cultureName, string expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            Assert.That(Localization.GetString("SaveGroup"), Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US", "HighscoreColumnName", "Name")]
    [TestCase("en-US", "HighscoreColumnWins", "Wins")]
    [TestCase("en-US", "HighscoreColumnPlayed", "Played")]
    [TestCase("en-US", "HighscoreColumnBest", "Best")]
    [TestCase("nl-NL", "HighscoreColumnName", "Naam")]
    [TestCase("nl-NL", "HighscoreColumnWins", "Winst")]
    [TestCase("nl-NL", "HighscoreColumnPlayed", "Gespeeld")]
    [TestCase("nl-NL", "HighscoreColumnBest", "Beste")]
    [TestCase("de-DE", "HighscoreColumnName", "Name")]
    [TestCase("de-DE", "HighscoreColumnWins", "Siege")]
    [TestCase("de-DE", "HighscoreColumnPlayed", "Spiele")]
    [TestCase("de-DE", "HighscoreColumnBest", "Beste")]
    [TestCase("es-ES", "HighscoreColumnName", "Nombre")]
    [TestCase("es-ES", "HighscoreColumnWins", "Ganadas")]
    [TestCase("es-ES", "HighscoreColumnPlayed", "Jugadas")]
    [TestCase("es-ES", "HighscoreColumnBest", "Mejor")]
    [TestCase("fr-FR", "HighscoreColumnName", "Nom")]
    [TestCase("fr-FR", "HighscoreColumnWins", "Gagn\u00e9es")]
    [TestCase("fr-FR", "HighscoreColumnPlayed", "Jou\u00e9es")]
    [TestCase("fr-FR", "HighscoreColumnBest", "Record")]
    public void HighscoreColumnHeaders_UseSelectedLanguage(string cultureName, string key, string expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            Assert.That(Localization.GetString(key), Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US", "Bid-not-total rule from round:")]
    [TestCase("nl-NL", "Bied-niet-totaal-regel vanaf ronde:")]
    [TestCase("de-DE", "Gebot-nicht-Summe-Regel ab Runde:")]
    [TestCase("es-ES", "Regla de apuesta distinta del total desde la ronda:")]
    [TestCase("fr-FR", "R\u00e8gle de mise diff\u00e9rente du total \u00e0 partir de la manche :")]
    public void BidTotalRuleStartRound_UsesBidNotTotalNameInSelectedLanguage(string cultureName, string expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            Assert.That(Localization.GetString("BidTotalRuleStartRound"), Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US", "Group name")]
    [TestCase("nl-NL", "Groepsnaam")]
    [TestCase("de-DE", "Gruppenname")]
    [TestCase("es-ES", "Nombre del grupo")]
    [TestCase("fr-FR", "Nom du groupe")]
    public void GroupName_UsesSelectedLanguage(string cultureName, string expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            Assert.That(Localization.GetString("GroupName"), Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US", "Dealer picks trump")]
    [TestCase("nl-NL", "Deler kiest troef")]
    [TestCase("de-DE", "Geber w\u00e4hlt Trumpf")]
    [TestCase("es-ES", "El repartidor elige el triunfo")]
    [TestCase("fr-FR", "Le donneur choisit l'atout")]
    public void AllowNoTrump_ShowsDealerPicksTrumpInSelectedLanguage(string cultureName, string expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            var caption = Localization.GetString("AllowNoTrump");

            Assert.That(caption, Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US", "Simplified buttons")]
    [TestCase("nl-NL", "Vereenvoudigde knoppen")]
    [TestCase("de-DE", "Vereinfachte Schaltfl\u00e4chen")]
    [TestCase("es-ES", "Botones simplificados")]
    [TestCase("fr-FR", "Boutons simplifi\u00e9s")]
    public void SimplifiedButtons_UsesSelectedLanguage(string cultureName, string expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            var caption = Localization.GetString("SimplifiedButtons");

            Assert.That(caption, Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US", "Don't forget to take screenshots of bugs and issues and attach them to your report!")]
    [TestCase("nl-NL", "Vergeet vooral niet screenshots van bugs en problemen te maken en bij je melding te voegen!")]
    [TestCase("de-DE", "Vergesst nicht, Screenshots von Fehlern und Problemen zu machen und eurem Bericht beizuf\u00fcgen!")]
    [TestCase("es-ES", "\u00a1No olvides hacer capturas de pantalla de los errores y problemas y adjuntarlas al informe!")]
    [TestCase("fr-FR", "N'oubliez surtout pas de faire des captures d'\u00e9cran des bugs et probl\u00e8mes et de les joindre au rapport !")]
    public void BetaWelcomeBody_RemindsTestersToTakeScreenshots(string cultureName, string reminder)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            var body = Localization.GetString("BetaWelcomeBody");

            Assert.That(body.ReplaceLineEndings("\n").Split("\n\n"), Does.Contain(reminder));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US")]
    [TestCase("nl-NL")]
    [TestCase("de-DE")]
    [TestCase("es-ES")]
    [TestCase("fr-FR")]
    public void BetaWelcomeBody_IncludesReportActionAndLocation(string cultureName)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var values = new[] { "[report-action]", "[report-location]" };

            var body = string.Format(Localization.GetString("BetaWelcomeBody"), values);

            Assert.That(Regex.Matches(body, @"\[report-[a-z]+\]").Select(match => match.Value),
                Is.EqualTo(values));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US", "BetaWelcomeTitle", "Welcome, beta testers!")]
    [TestCase("nl-NL", "BetaWelcomeTitle", "Welkom, b\u00e8tatesters!")]
    [TestCase("de-DE", "BetaWelcomeTitle", "Willkommen, Betatester!")]
    [TestCase("es-ES", "BetaWelcomeTitle", "\u00a1Bienvenidos, probadores de la beta!")]
    [TestCase("fr-FR", "BetaWelcomeTitle", "Bienvenue aux b\u00eata-testeurs !")]
    [TestCase("en-US", "BetaWelcomeDoNotShowAgain", "Don't show this beta information again")]
    [TestCase("nl-NL", "BetaWelcomeDoNotShowAgain", "Deze b\u00e8ta-informatie niet opnieuw tonen")]
    [TestCase("de-DE", "BetaWelcomeDoNotShowAgain", "Diese Beta-Informationen nicht erneut anzeigen")]
    [TestCase("es-ES", "BetaWelcomeDoNotShowAgain", "No volver a mostrar esta informaci\u00f3n de la beta")]
    [TestCase("fr-FR", "BetaWelcomeDoNotShowAgain", "Ne plus afficher ces informations sur la b\u00eata")]
    [TestCase("en-US", "BetaWelcomeClose", "Close and continue")]
    [TestCase("nl-NL", "BetaWelcomeClose", "Sluiten en doorgaan")]
    [TestCase("de-DE", "BetaWelcomeClose", "Schlie\u00dfen und fortfahren")]
    [TestCase("es-ES", "BetaWelcomeClose", "Cerrar y continuar")]
    [TestCase("fr-FR", "BetaWelcomeClose", "Fermer et continuer")]
    public void BetaWelcomeLabel_UsesSelectedLanguage(string cultureName, string key, string expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            var label = Localization.GetString(key);

            Assert.That(label, Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US", "Technical information (automatically added; review before sending):")]
    [TestCase("nl-NL", "Technische informatie (automatisch toegevoegd; controleer voor verzending):")]
    [TestCase("de-DE", "Technische Informationen (automatisch hinzugef\u00fcgt; vor dem Senden pr\u00fcfen):")]
    [TestCase("es-ES", "Informaci\u00f3n t\u00e9cnica (a\u00f1adida autom\u00e1ticamente; rev\u00edsala antes de enviarla):")]
    [TestCase("fr-FR", "Informations techniques (ajout\u00e9es automatiquement ; \u00e0 v\u00e9rifier avant l'envoi) :")]
    public void BugReportBodyTemplate_UsesEnglishDetailsBelowLocalizedHeading(string cultureName, string heading)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var expectedDetails = $"""
                App version: 2.1.0 (build 12345)
                OS: Windows
                OS version: 10.0
                Manufacturer: Contoso
                Model: ModelX
                Device category: Desktop
                Device type (physical/virtual): Physical
                Screen size (pixels): 1920 x 1080
                Screen size (logical units): 1536 x 864
                Pixel density: 1.25
                Orientation: Landscape
                Refresh rate (Hz): 59.94
                App language: {cultureName}
                """;

            var body = string.Format(CultureInfo.InvariantCulture,
                Localization.GetString("BugReportBodyTemplate"),
                "2.1.0", "12345", "Windows", "10.0", "Contoso", "ModelX", "Desktop", "Physical",
                "1920 x 1080", "1536 x 864", 1.25, "Landscape", 59.94, cultureName);

            Assert.That(body.ReplaceLineEndings("\n"),
                Does.EndWith($"{heading}\n{expectedDetails.ReplaceLineEndings("\n")}"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US", "BugReportCopy", "Copy report")]
    [TestCase("nl-NL", "BugReportCopy", "Rapport kopi\u00ebren")]
    [TestCase("de-DE", "BugReportCopy", "Bericht kopieren")]
    [TestCase("es-ES", "BugReportCopy", "Copiar informe")]
    [TestCase("fr-FR", "BugReportCopy", "Copier le rapport")]
    [TestCase("en-US", "BugReportOpenEmail", "Open email app")]
    [TestCase("nl-NL", "BugReportOpenEmail", "E-mailapp openen")]
    [TestCase("de-DE", "BugReportOpenEmail", "E-Mail-App \u00f6ffnen")]
    [TestCase("es-ES", "BugReportOpenEmail", "Abrir aplicaci\u00f3n de correo")]
    [TestCase("fr-FR", "BugReportOpenEmail", "Ouvrir la messagerie")]
    public void BugReportAction_UsesSelectedLanguage(string cultureName, string key, string expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            var label = Localization.GetString(key);

            Assert.That(label, Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US", "To", "Subject")]
    [TestCase("nl-NL", "Aan", "Onderwerp")]
    [TestCase("de-DE", "An", "Betreff")]
    [TestCase("es-ES", "Para", "Asunto")]
    [TestCase("fr-FR", "Destinataire ", "Objet ")]
    public void BugReportClipboardTemplate_PreservesRecipientSubjectAndPlainTextBody(
        string cultureName, string recipientLabel, string subjectLabel)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            const string recipient = "nodorumsolutio@gmail.com";
            const string subject = "Bug report Wizard Scoreboard 2.1.0";
            const string body = "Problem: A&B + C? #1\r\nDevice: Example {model}";

            var text = string.Format(CultureInfo.CurrentCulture,
                Localization.GetString("BugReportClipboardTemplate"), recipient, subject, body)
                .ReplaceLineEndings("\r\n");

            Assert.That(text, Is.EqualTo(
                $"{recipientLabel}: {recipient}\r\n{subjectLabel}: {subject}\r\n\r\n{body}"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US")]
    [TestCase("nl-NL")]
    [TestCase("de-DE")]
    [TestCase("es-ES")]
    [TestCase("fr-FR")]
    public void BugReportBodyTemplate_IncludesEveryDiagnosticValueInOrder(string cultureName)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var values = Enumerable.Range(0, 14).Select(i => $"[diagnostic-{i}]").ToArray();

            var body = string.Format(CultureInfo.CurrentCulture,
                Localization.GetString("BugReportBodyTemplate"), values);

            Assert.That(Regex.Matches(body, @"\[diagnostic-\d+\]").Select(match => match.Value),
                Is.EqualTo(values));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US")]
    [TestCase("nl-NL")]
    [TestCase("de-DE")]
    [TestCase("es-ES")]
    [TestCase("fr-FR")]
    public void BugReportSubject_IncludesAppVersionInEveryLanguage(string cultureName)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            var subject = string.Format(CultureInfo.InvariantCulture,
                Localization.GetString("BugReportSubject"), "3.4.5");

            Assert.That(subject, Is.EqualTo("Bug report Wizard Scoreboard 3.4.5"));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("en-US", "Report a bug")]
    [TestCase("nl-NL", "Een bug melden")]
    [TestCase("de-DE", "Fehler melden")]
    [TestCase("es-ES", "Informar de un error")]
    [TestCase("fr-FR", "Signaler un bug")]
    public void ReportBug_UsesSelectedLanguage(string cultureName, string expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            var label = Localization.GetString("ReportBug");

            Assert.That(label, Is.EqualTo(expected));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

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

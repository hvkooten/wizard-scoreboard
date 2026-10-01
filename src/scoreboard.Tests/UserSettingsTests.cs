using System;
using Microsoft.Maui.ApplicationModel;
using NUnit.Framework;
using WizardScoreboard.Models;
using WizardScoreboard.Services;

namespace WizardScoreboard.Tests;

public class UserSettingsTests
{
    [Test]
    public void Constructor_NullPreferencesThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new UserSettings(null!));
    }

    [Test]
    public void NewSettings_HasNoLanguageOverride()
    {
        Assert.That(new UserSettings(new MemoryPreferences()).Language, Is.Null);
    }

    [Test]
    public void NewSettings_RequiresFirstLaunchSetup()
    {
        Assert.That(new UserSettings(new MemoryPreferences()).IsFirstLaunch, Is.True);
    }

    [Test]
    public void NewSettings_FollowsSystemTheme()
    {
        Assert.That(new UserSettings(new MemoryPreferences()).Theme, Is.EqualTo(AppTheme.Unspecified));
    }

    [Test]
    public void NewSettings_DoesNotKeepScreenAwake()
    {
        Assert.That(new UserSettings(new MemoryPreferences()).KeepScreenAwakeDuringGame, Is.False);
    }

    [Test]
    public void NewSettings_DoesNotEnableBoldUntilDefaultsAreApplied()
    {
        Assert.That(new UserSettings(new MemoryPreferences()).BoldAllText, Is.False);
    }

    [Test]
    public void NewSettings_UsesPlayerCountBidRule()
    {
        Assert.That(new UserSettings(new MemoryPreferences()).DefaultBidTotalRuleStartRound, Is.EqualTo(Group.PlayerCountRule));
    }

    [TestCase("nl-NL")]
    [TestCase("en-US")]
    [TestCase(null)]
    public void Language_PersistsAcrossSettingsInstances(string? language)
    {
        var preferences = new MemoryPreferences();
        var settings = new UserSettings(preferences) { Language = "fr-FR" };

        settings.Language = language;

        Assert.That(new UserSettings(preferences).Language, Is.EqualTo(language));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void IsFirstLaunch_PersistsAcrossSettingsInstances(bool firstLaunch)
    {
        var preferences = new MemoryPreferences();
        var settings = new UserSettings(preferences);

        settings.IsFirstLaunch = firstLaunch;

        Assert.That(new UserSettings(preferences).IsFirstLaunch, Is.EqualTo(firstLaunch));
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void IsFirstLaunch_StoresInverseCompletedFlag(bool firstLaunch, bool expectedStored)
    {
        var preferences = new MemoryPreferences();
        var settings = new UserSettings(preferences);

        settings.IsFirstLaunch = firstLaunch;

        Assert.That(preferences.Get("first_launch_done", !expectedStored), Is.EqualTo(expectedStored));
    }

    [TestCase(AppTheme.Unspecified)]
    [TestCase(AppTheme.Light)]
    [TestCase(AppTheme.Dark)]
    public void Theme_PersistsAcrossSettingsInstances(AppTheme theme)
    {
        var preferences = new MemoryPreferences();
        var settings = new UserSettings(preferences);

        settings.Theme = theme;

        Assert.That(new UserSettings(preferences).Theme, Is.EqualTo(theme));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void KeepScreenAwake_PersistsAcrossSettingsInstances(bool enabled)
    {
        var preferences = new MemoryPreferences();
        var settings = new UserSettings(preferences);

        settings.KeepScreenAwakeDuringGame = enabled;

        Assert.That(new UserSettings(preferences).KeepScreenAwakeDuringGame, Is.EqualTo(enabled));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void BoldAllText_PersistsAcrossSettingsInstances(bool enabled)
    {
        var preferences = new MemoryPreferences();
        var settings = new UserSettings(preferences);

        settings.BoldAllText = enabled;

        Assert.That(new UserSettings(preferences).BoldAllText, Is.EqualTo(enabled));
    }

    [TestCase(Group.PlayerCountRule)]
    [TestCase(Group.DoublePlayerCountRule)]
    [TestCase(0)]
    [TestCase(20)]
    public void DefaultBidRule_PersistsAcrossSettingsInstances(int rule)
    {
        var preferences = new MemoryPreferences();
        var settings = new UserSettings(preferences);

        settings.DefaultBidTotalRuleStartRound = rule;

        Assert.That(new UserSettings(preferences).DefaultBidTotalRuleStartRound, Is.EqualTo(rule));
    }

    [Test]
    public void ApplyFirstLaunchDefaults_EnablesBoldForNewUsers()
    {
        var settings = new UserSettings(new MemoryPreferences());

        settings.ApplyFirstLaunchDefaults();

        Assert.That(settings.BoldAllText, Is.True);
    }

    [Test]
    public void ApplyFirstLaunchDefaults_DoesNotMarkSetupCompleted()
    {
        var settings = new UserSettings(new MemoryPreferences());

        settings.ApplyFirstLaunchDefaults();

        Assert.That(settings.IsFirstLaunch, Is.True);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ApplyFirstLaunchDefaults_PreservesReturningUsersBoldChoice(bool bold)
    {
        var settings = new UserSettings(new MemoryPreferences()) { IsFirstLaunch = false, BoldAllText = bold };

        settings.ApplyFirstLaunchDefaults();

        Assert.That(settings.BoldAllText, Is.EqualTo(bold));
    }

    [Test]
    public void Settings_ReadExistingPreferenceKeysWithoutMigration()
    {
        var preferences = new MemoryPreferences();
        preferences.Set("language", "de-DE");
        preferences.Set("first_launch_done", true);
        preferences.Set("app_theme", (int)AppTheme.Dark);
        preferences.Set("keep_screen_awake", true);
        preferences.Set("bold_all_text", true);
        preferences.Set("default_bid_total_rule", 9);

        var settings = new UserSettings(preferences);

        Assert.That((settings.Language, settings.IsFirstLaunch, settings.Theme,
                settings.KeepScreenAwakeDuringGame, settings.BoldAllText, settings.DefaultBidTotalRuleStartRound),
            Is.EqualTo(("de-DE", false, AppTheme.Dark, true, true, 9)));
    }
}

using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

// App-wide defaults that are not tied to a specific group.
public static class AppSettings
{
    private const string DefaultBidTotalRuleKey = "default_bid_total_rule";
    private const string KeepScreenAwakeKey = "keep_screen_awake";
    private const string BoldAllTextKey = "bold_all_text";
    private const string AppThemeKey = "app_theme";
    private const string FirstLaunchKey = "first_launch_done";

    // True until the app has been opened once, so the settings page can be shown first.
    public static bool IsFirstLaunch
    {
        get => !Preferences.Default.Get(FirstLaunchKey, false);
        set => Preferences.Default.Set(FirstLaunchKey, !value);
    }

    // User-selected theme. AppTheme.Unspecified means "follow the system setting".
    public static AppTheme Theme
    {
        get => (AppTheme)Preferences.Default.Get(AppThemeKey, (int)AppTheme.Unspecified);
        set => Preferences.Default.Set(AppThemeKey, (int)value);
    }

    // When true, the device screen is kept on while a game is in progress.
    public static bool KeepScreenAwakeDuringGame
    {
        get => Preferences.Default.Get(KeepScreenAwakeKey, false);
        set => Preferences.Default.Set(KeepScreenAwakeKey, value);
    }

    // When true, all label text throughout the app is rendered bold.
    public static bool BoldAllText
    {
        get => Preferences.Default.Get(BoldAllTextKey, false);
        set => Preferences.Default.Set(BoldAllTextKey, value);
    }

    // Default bid-total-rule start round applied when creating a new group.
    // Group.PlayerCountRule means "follow the number of players".
    public static int DefaultBidTotalRuleStartRound
    {
        get => Preferences.Default.Get(DefaultBidTotalRuleKey, Group.PlayerCountRule);
        set => Preferences.Default.Set(DefaultBidTotalRuleKey, value);
    }
}

namespace WizardScoreboard.Services;

// App-wide defaults that are not tied to a specific group.
public static class AppSettings
{
    private static readonly UserSettings settings = new(Preferences.Default);

    // Culture name chosen by the user, or null when the system language should be used.
    public static string? Language
    {
        get => settings.Language;
        set => settings.Language = value;
    }

    // Applies the defaults for a fresh install: bold text.
    public static void ApplyFirstLaunchDefaults() => settings.ApplyFirstLaunchDefaults();

    // True until the app has been opened once, so the settings page can be shown first.
    public static bool IsFirstLaunch
    {
        get => settings.IsFirstLaunch;
        set => settings.IsFirstLaunch = value;
    }

    // User-selected theme. AppTheme.Unspecified means "follow the system setting".
    public static AppTheme Theme
    {
        get => settings.Theme;
        set => settings.Theme = value;
    }

    // When true, the device screen is kept on while a game is in progress.
    public static bool KeepScreenAwakeDuringGame
    {
        get => settings.KeepScreenAwakeDuringGame;
        set => settings.KeepScreenAwakeDuringGame = value;
    }

    // When true, all label text throughout the app is rendered bold.
    public static bool BoldAllText
    {
        get => settings.BoldAllText;
        set => settings.BoldAllText = value;
    }

    // Default bid-total-rule start round applied when creating a new group.
    // Group.PlayerCountRule means "follow the number of players".
    public static int DefaultBidTotalRuleStartRound
    {
        get => settings.DefaultBidTotalRuleStartRound;
        set => settings.DefaultBidTotalRuleStartRound = value;
    }
}

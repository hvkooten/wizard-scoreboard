using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

/// <summary>
/// Reads and writes application settings using the supplied preference storage.
/// </summary>
public sealed class UserSettings
{
    private const string DefaultBidTotalRuleKey = "default_bid_total_rule";
    private const string KeepScreenAwakeKey = "keep_screen_awake";
    private const string BoldAllTextKey = "bold_all_text";
    private const string AppThemeKey = "app_theme";
    private const string FirstLaunchKey = "first_launch_done";
    private const string LanguageKey = "language";
    private const string ShowBetaWelcomeKey = "show_beta_welcome";
    private readonly IPreferences preferences;

    /// <summary>
    /// Creates settings backed by the supplied preference storage.
    /// </summary>
    public UserSettings(IPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        this.preferences = preferences;
    }

    /// <summary>Gets or sets the selected culture, or null to follow the system language.</summary>
    public string? Language
    {
        get => preferences.Get<string?>(LanguageKey, null);
        set => preferences.Set(LanguageKey, value);
    }

    /// <summary>Applies initial defaults without overwriting returning users' preferences.</summary>
    public void ApplyFirstLaunchDefaults()
    {
        if (IsFirstLaunch)
            BoldAllText = true;
    }

    /// <summary>Gets or sets whether first-launch setup is still required.</summary>
    public bool IsFirstLaunch
    {
        get => !preferences.Get(FirstLaunchKey, false);
        set => preferences.Set(FirstLaunchKey, !value);
    }

    /// <summary>Gets or sets whether beta information is shown after the splash screen.</summary>
    public bool ShowBetaWelcome
    {
        get => preferences.Get(ShowBetaWelcomeKey, true);
        set => preferences.Set(ShowBetaWelcomeKey, value);
    }

    /// <summary>Gets or sets the preferred theme.</summary>
    public AppTheme Theme
    {
        get => (AppTheme)preferences.Get(AppThemeKey, (int)AppTheme.Unspecified);
        set => preferences.Set(AppThemeKey, (int)value);
    }

    /// <summary>Gets or sets whether an active game should keep the screen awake.</summary>
    public bool KeepScreenAwakeDuringGame
    {
        get => preferences.Get(KeepScreenAwakeKey, false);
        set => preferences.Set(KeepScreenAwakeKey, value);
    }

    /// <summary>Gets or sets whether all text should be bold.</summary>
    public bool BoldAllText
    {
        get => preferences.Get(BoldAllTextKey, false);
        set => preferences.Set(BoldAllTextKey, value);
    }

    /// <summary>Gets or sets the default bid-total rule for new groups.</summary>
    public int DefaultBidTotalRuleStartRound
    {
        get => preferences.Get(DefaultBidTotalRuleKey, Group.PlayerCountRule);
        set => preferences.Set(DefaultBidTotalRuleKey, value);
    }
}

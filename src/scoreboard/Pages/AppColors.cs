namespace WizardScoreboard.Pages;

// Shared colors reused across multiple pages. Page-specific one-off colors stay local.
// Every color resolves against the active light/dark theme at the moment it is read, so pages
// must be rebuilt (see App.ApplyTheme) to pick up a theme change.
internal static class AppColors
{
    public static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

    private static Color Pick(string light, string dark) => Color.FromArgb(IsDark ? dark : light);

    // Primary dark-blue theme color (headers, active toggles, accent buttons).
    public static Color Primary => Pick("#1a3a5c", "#2d5a8a");

    // Inactive toggle-button background and its text color.
    public static Color ToggleInactiveBg => Pick("#e0e8f0", "#243446");
    public static Color ToggleInactiveText => Pick("#1a3a5c", "#cfe0f2");

    // Danger/delete background and text (also used for "lost round" cells).
    public static Color DangerBg => Pick("#fde8e8", "#4a1c1c");
    public static Color DangerText => Pick("#a32020", "#ff8a8a");

    // Subtle border/stroke color for cards and cells.
    public static Color Border => Pick("#d6dce5", "#34465a");

    // Android nav-tab colors (active/inactive background and text).
    public static Color NavActiveBg => Pick("#d7e9ff", "#24476e");
    public static Color NavInactiveBg => Pick("#edf4ff", "#1a2c40");
    public static Color NavText => Pick("#163a5f", "#d7e9ff");

    // Page/surface backgrounds.
    public static Color Surface => Pick("#ffffff", "#121c27");
    public static Color SurfaceAlt => Pick("#f0f4f8", "#1a2633");
    public static Color Card => Pick("#f8fbff", "#1a2633");
    public static Color Flyout => Pick("#e8e8e8", "#1a2633");

    // Background for input controls (entries, pickers); native defaults are light grey in dark mode.
    public static Color InputBackground => Pick("#ffffff", "#243446");

    // Foreground text colors.
    public static Color TextPrimary => Pick("#000000", "#e6edf5");
    public static Color TextMuted => Pick("#808080", "#9aa8b8");
    public static Color SuccessText => Pick("#006400", "#8fe39f");
    public static Color WarningText => Pick("#8b0000", "#ffb0b0");

    // Highlight for the dealer's name in the bid/actuals forms.
    public static Color DealerText => Pick("#b26a00", "#ffd27a");

    // Fiery page-title color.
    public static Color TitleText => Pick("#8A1C0A", "#ff9a6a");
}

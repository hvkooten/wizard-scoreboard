namespace WizardScoreboard.Pages;

// Shared colors reused across multiple pages. Page-specific one-off colors stay local.
internal static class AppColors
{
    // Primary dark-blue theme color (headers, active toggles, accent buttons).
    public static readonly Color Primary = Color.FromArgb("#1a3a5c");

    // Inactive toggle-button background.
    public static readonly Color ToggleInactiveBg = Color.FromArgb("#e0e8f0");

    // Danger/delete background and text (also used for "lost round" cells).
    public static readonly Color DangerBg = Color.FromArgb("#fde8e8");
    public static readonly Color DangerText = Color.FromArgb("#a32020");

    // Subtle border/stroke color for cards and cells.
    public static readonly Color Border = Color.FromArgb("#d6dce5");

    // Android nav-tab colors (active/inactive background and text).
    public static readonly Color NavActiveBg = Color.FromArgb("#d7e9ff");
    public static readonly Color NavInactiveBg = Color.FromArgb("#edf4ff");
    public static readonly Color NavText = Color.FromArgb("#163a5f");
}

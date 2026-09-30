using Microsoft.Maui.Controls;
using WizardScoreboard.Pages;
using WizardScoreboard.Services;

namespace WizardScoreboard;

public class App : Application
{
    private const string PrefWidth = "window_width_v1";
    private const string PrefHeight = "window_height_v1";

    private readonly AppShell shell;
    private readonly IScreenWakeService screenWakeService;

    // Raised whenever the global text style is (re)applied, e.g. when "bold all text" is toggled.
    // Pages that build their content dynamically can subscribe to rebuild themselves immediately.
    public static event Action? GlobalTextStyleChanged;

    public App(AppShell shell, IScreenWakeService screenWakeService)
    {
        this.shell = shell;
        this.screenWakeService = screenWakeService;

        ApplyGlobalTextStyle();
    }

    // Applies the app-wide bold-text preference (AppSettings.BoldAllText) directly to the
    // currently displayed page's visual tree. An implicit Style is not used here: MAUI does not
    // reliably re-evaluate implicit styles on controls that are already alive when the style
    // dictionary is added or removed, so toggling the setting would not consistently take effect
    // (or revert) on existing pages. Setting FontAttributes directly avoids that limitation.
    public static void ApplyGlobalTextStyle()
    {
        // Native Shell tab bar text ignores MAUI styles/attributes, so update it explicitly.
        (Shell.Current as AppShell)?.ApplyTabBarTextStyle();

        ApplyBoldToVisualTree(Shell.Current?.CurrentPage, AppSettings.BoldAllText);

        // Notify subscribers so already-rendered pages can rebuild their dynamic content, e.g. to
        // pick up the current bold state for items created after this call.
        GlobalTextStyleChanged?.Invoke();
    }

    // Recursively walks the visual tree, setting FontAttributes on font-bearing controls.
    // HeaderLabel is deliberately excluded: the Shell header always stays bold regardless of
    // the setting.
    internal static void ApplyBoldToVisualTree(IVisualTreeElement? root, bool bold)
    {
        if (root is null)
        {
            return;
        }

        var attributes = bold ? FontAttributes.Bold : FontAttributes.None;

        switch (root)
        {
            case HeaderLabel:
                break;
            case Label label:
                label.FontAttributes = attributes;
                break;
            case Button button:
                button.FontAttributes = attributes;
                break;
            case Entry entry:
                entry.FontAttributes = attributes;
                break;
            case Editor editor:
                editor.FontAttributes = attributes;
                break;
            case Picker picker:
                picker.FontAttributes = attributes;
                break;
            case DatePicker datePicker:
                datePicker.FontAttributes = attributes;
                break;
            case TimePicker timePicker:
                timePicker.FontAttributes = attributes;
                break;
            case SearchBar searchBar:
                searchBar.FontAttributes = attributes;
                break;
        }

        foreach (var child in root.GetVisualChildren())
        {
            ApplyBoldToVisualTree(child, bold);
        }
    }

    // Release the wake lock when the app is no longer in the foreground.
    protected override void OnSleep() => screenWakeService.Suspend();

    // Restore the wake lock based on the current game state and setting.
    protected override void OnResume() => screenWakeService.Refresh();

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(shell);

        // Keep splash screen visible for 1.5 seconds on app startup
        window.Created += async (s, e) =>
        {
            await Task.Delay(1500);
        };

        var savedWidth = Preferences.Default.Get(PrefWidth, 0.0);
        var savedHeight = Preferences.Default.Get(PrefHeight, 0.0);

        if (savedWidth > 0 && savedHeight > 0)
        {
            window.Width = savedWidth;
            window.Height = savedHeight;
        }

        window.SizeChanged += (s, e) =>
        {
            if (window.Width > 0 && window.Height > 0)
            {
                Preferences.Default.Set(PrefWidth, window.Width);
                Preferences.Default.Set(PrefHeight, window.Height);
            }
        };

        return window;
    }
}

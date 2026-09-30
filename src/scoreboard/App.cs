using Microsoft.Maui.Controls;
using WizardScoreboard.Pages;
using WizardScoreboard.Services;

namespace WizardScoreboard;

public class App : Application
{
    private const string PrefWidth = "window_width_v1";
    private const string PrefHeight = "window_height_v1";

    private readonly IServiceProvider services;
    private readonly IScreenWakeService screenWakeService;

    // Raised whenever the global text style is (re)applied, e.g. when "bold all text" is toggled.
    // Pages that build their content dynamically can subscribe to rebuild themselves immediately.
    public static event Action? GlobalTextStyleChanged;

    public App(IServiceProvider services, IScreenWakeService screenWakeService)
    {
        this.services = services;
        this.screenWakeService = screenWakeService;

        UserAppTheme = AppSettings.Theme;
        ApplyThemeResources();
        // Page colors are resolved when pages are built, so rebuild the UI when the system theme
        // changes while the user follows the system setting.
        RequestedThemeChanged += (s, e) =>
        {
            ApplyThemeResources();
            RebuildShell();
        };

        ApplyGlobalTextStyle();
    }

    // Registers implicit styles with the current theme colors. Platform defaults do not reliably
    // follow UserAppTheme (e.g. black text on Windows), so every control gets explicit colors.
    // Styles are recreated on each theme change and picked up by the rebuilt pages.
    private void ApplyThemeResources()
    {
        Resources = new ResourceDictionary
        {
            CreateStyle<Page>(
                (Page.BackgroundColorProperty, AppColors.Surface)),
            CreateStyle<Label>(
                (Label.TextColorProperty, AppColors.TextPrimary)),
            CreateStyle<Button>(
                (Button.BackgroundColorProperty, AppColors.Primary),
                (Button.TextColorProperty, Colors.White)),
            CreateStyle<Entry>(
                (Entry.BackgroundColorProperty, AppColors.InputBackground),
                (Entry.TextColorProperty, AppColors.TextPrimary),
                (Entry.PlaceholderColorProperty, AppColors.TextMuted)),
            CreateStyle<Editor>(
                (Editor.BackgroundColorProperty, AppColors.InputBackground),
                (Editor.TextColorProperty, AppColors.TextPrimary),
                (Editor.PlaceholderColorProperty, AppColors.TextMuted)),
            CreateStyle<Picker>(
                (Picker.BackgroundColorProperty, AppColors.InputBackground),
                (Picker.TextColorProperty, AppColors.TextPrimary),
                (Picker.TitleColorProperty, AppColors.TextMuted)),
            CreateStyle<DatePicker>(
                (DatePicker.BackgroundColorProperty, AppColors.InputBackground),
                (DatePicker.TextColorProperty, AppColors.TextPrimary)),
            CreateStyle<TimePicker>(
                (TimePicker.BackgroundColorProperty, AppColors.InputBackground),
                (TimePicker.TextColorProperty, AppColors.TextPrimary)),
            CreateStyle<SearchBar>(
                (SearchBar.BackgroundColorProperty, AppColors.InputBackground),
                (SearchBar.TextColorProperty, AppColors.TextPrimary),
                (SearchBar.PlaceholderColorProperty, AppColors.TextMuted)),
            CreateStyle<CheckBox>(
                (CheckBox.ColorProperty, AppColors.Primary)),
            CreateStyle<Shell>(
                (Shell.BackgroundColorProperty, AppColors.Surface),
                (Shell.ForegroundColorProperty, AppColors.TextPrimary),
                (Shell.TitleColorProperty, AppColors.TextPrimary),
                (Shell.TabBarBackgroundColorProperty, AppColors.NavInactiveBg),
                (Shell.TabBarForegroundColorProperty, AppColors.NavText),
                (Shell.TabBarTitleColorProperty, AppColors.NavText),
                (Shell.TabBarUnselectedColorProperty, AppColors.TextMuted))
        };
    }

    private static Style CreateStyle<T>(params (BindableProperty Property, object Value)[] setters)
    {
        var style = new Style(typeof(T)) { ApplyToDerivedTypes = true };
        foreach (var (property, value) in setters)
        {
            style.Setters.Add(new Setter { Property = property, Value = value });
        }

        return style;
    }

    // Applies and persists the selected theme and rebuilds the UI so all pages pick up the new colors.
    public static void ApplyTheme(AppTheme theme)
    {
        AppSettings.Theme = theme;
        if (Current is null)
        {
            return;
        }

        // Setting UserAppTheme raises RequestedThemeChanged when the effective theme changes,
        // which rebuilds the shell.
        Current.UserAppTheme = theme;
    }

    // Replaces the window's root page with a fresh AppShell so every page is rebuilt.
    private static void RebuildShell()
    {
        var shell = Current?.Handler?.MauiContext?.Services.GetService<AppShell>();
        var window = Current?.Windows.FirstOrDefault();
        if (shell != null && window != null)
        {
            window.Page = shell;
        }
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
        // Resolve the shell here (not in the constructor) so pages are built after the theme is set.
        var window = new Window(services.GetRequiredService<AppShell>());

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

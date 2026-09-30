using Microsoft.Maui.Controls;
using WizardScoreboard.Services;

namespace WizardScoreboard;

public class App : Application
{
    private const string PrefWidth = "window_width_v1";
    private const string PrefHeight = "window_height_v1";

    private readonly AppShell shell;
    private readonly IScreenWakeService screenWakeService;

    // Holds the currently applied bold-text implicit style so it can be toggled at runtime.
    private static ResourceDictionary? boldTextDictionary;

    // Raised whenever the global text style is (re)applied, e.g. when "bold all text" is toggled.
    // Pages that build their content dynamically can subscribe to rebuild themselves immediately.
    public static event Action? GlobalTextStyleChanged;

    public App(AppShell shell, IScreenWakeService screenWakeService)
    {
        this.shell = shell;
        this.screenWakeService = screenWakeService;

        ApplyGlobalTextStyle();
    }

    // Applies (or removes) an app-wide implicit Label style that renders all text bold,
    // based on the AppSettings.BoldAllText preference. Safe to call repeatedly.
    public static void ApplyGlobalTextStyle()
    {
        var resources = Current?.Resources;
        if (resources is null)
        {
            return;
        }

        // Native Shell tab bar text ignores implicit styles, so update it explicitly.
        (Shell.Current as AppShell)?.ApplyTabBarTextStyle();

        if (boldTextDictionary is not null)
        {
            resources.MergedDictionaries.Remove(boldTextDictionary);
            boldTextDictionary = null;
        }

        if (!AppSettings.BoldAllText)
        {
            // Notify subscribers so already-rendered pages can rebuild without the bold style.
            GlobalTextStyleChanged?.Invoke();
            return;
        }

        var boldLabelStyle = new Style(typeof(Label));
        boldLabelStyle.Setters.Add(new Setter
        {
            Property = Label.FontAttributesProperty,
            Value = FontAttributes.Bold
        });

        boldTextDictionary = new ResourceDictionary();
        boldTextDictionary.Add(boldLabelStyle);

        // Labels are not the only text-bearing controls; apply bold to the other common
        // text controls too so the "bold all text" setting is truly app-wide.
        foreach (var controlType in new[]
        {
            typeof(Button),
            typeof(Entry),
            typeof(Editor),
            typeof(Picker),
            typeof(DatePicker),
            typeof(TimePicker),
            typeof(SearchBar)
        })
        {
            var style = new Style(controlType);
            style.Setters.Add(new Setter
            {
                Property = FontElementFontAttributesProperty(controlType),
                Value = FontAttributes.Bold
            });
            boldTextDictionary.Add(style);
        }

        resources.MergedDictionaries.Add(boldTextDictionary);

        // Notify subscribers so already-rendered pages can rebuild with the bold style applied.
        GlobalTextStyleChanged?.Invoke();
    }

    // Resolves the FontAttributes bindable property for a given text control type.
    private static BindableProperty FontElementFontAttributesProperty(Type controlType) => controlType switch
    {
        _ when controlType == typeof(Button) => Button.FontAttributesProperty,
        _ when controlType == typeof(Entry) => Entry.FontAttributesProperty,
        _ when controlType == typeof(Editor) => Editor.FontAttributesProperty,
        _ when controlType == typeof(Picker) => Picker.FontAttributesProperty,
        _ when controlType == typeof(DatePicker) => DatePicker.FontAttributesProperty,
        _ when controlType == typeof(TimePicker) => TimePicker.FontAttributesProperty,
        _ when controlType == typeof(SearchBar) => SearchBar.FontAttributesProperty,
        _ => Label.FontAttributesProperty
    };

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

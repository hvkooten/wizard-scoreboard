using Microsoft.Maui.Controls;

namespace WizardScoreboard;

public class App : Application
{
    private const string PrefWidth  = "window_width_v1";
    private const string PrefHeight = "window_height_v1";

    private readonly AppShell shell;

    public App(AppShell shell)
    {
        this.shell = shell;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(shell);

        // Keep splash screen visible for 1.5 seconds on app startup
        window.Created += async (s, e) =>
        {
            await Task.Delay(1500);
        };

        var savedWidth  = Preferences.Default.Get(PrefWidth,  0.0);
        var savedHeight = Preferences.Default.Get(PrefHeight, 0.0);

        if (savedWidth > 0 && savedHeight > 0)
        {
            window.Width  = savedWidth;
            window.Height = savedHeight;
        }

        window.SizeChanged += (s, e) =>
        {
            if (window.Width > 0 && window.Height > 0)
            {
                Preferences.Default.Set(PrefWidth,  window.Width);
                Preferences.Default.Set(PrefHeight, window.Height);
            }
        };

        return window;
    }
}

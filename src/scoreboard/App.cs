using Microsoft.Maui.Controls;

namespace WizardScoreboard;

public class App : Application
{
    private readonly AppShell shell;

    public App(AppShell shell)
    {
        this.shell = shell;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(shell);
    }
}

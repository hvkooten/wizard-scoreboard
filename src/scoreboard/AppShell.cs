using Microsoft.Maui.Controls;

namespace WizardScoreboard;

public class AppShell : Shell
{
    public AppShell()
    {
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(RulesPage), typeof(RulesPage));
        Routing.RegisterRoute(nameof(HighscorePage), typeof(HighscorePage));
        Routing.RegisterRoute(nameof(ScoreBoardPage), typeof(ScoreBoardPage));

        Items.Add(new TabBar
        {
            Items =
            {
                new Tab
                {
                    Title = "Instellingen",
                    IconImageSource = "settings.png",
                    Items =
                    {
                        new ShellContent { Content = new SettingsPage() }
                    }
                },
                new Tab
                {
                    Title = "Spelregels",
                    IconImageSource = "book.png",
                    Items =
                    {
                        new ShellContent { Content = new RulesPage() }
                    }
                },
                new Tab
                {
                    Title = "Highscore",
                    IconImageSource = "trophy.png",
                    Items =
                    {
                        new ShellContent { Content = new HighscorePage() }
                    }
                },
                new Tab
                {
                    Title = "Scoreblok",
                    IconImageSource = "score.png",
                    Items =
                    {
                        new ShellContent { Content = new ScoreBoardPage() }
                    }
                }
            }
        });
    }
}

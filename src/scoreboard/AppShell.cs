using Microsoft.Maui.Controls;
using WizardScoreboard.Pages;
using WizardScoreboard.Resources;

namespace WizardScoreboard;

public class AppShell : Shell
{
    public AppShell(SettingsPage settingsPage, RulesPage rulesPage, HighscorePage highscorePage, SavedGamesPage savedGamesPage, ScoreBoardPage scoreBoardPage)
    {
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(RulesPage), typeof(RulesPage));
        Routing.RegisterRoute(nameof(HighscorePage), typeof(HighscorePage));
        Routing.RegisterRoute(nameof(SavedGamesPage), typeof(SavedGamesPage));
        Routing.RegisterRoute(nameof(ScoreBoardPage), typeof(ScoreBoardPage));

        Items.Add(new TabBar
        {
            Items =
            {
                new Tab
                {
                    Title = Localization.GetString("Scoreboard"),
                    Items =
                    {
                        new ShellContent { Content = scoreBoardPage }
                    }
                },
                new Tab
                {
                    Title = Localization.GetString("Highscore"),
                    Items =
                    {
                        new ShellContent { Content = highscorePage }
                    }
                },
                new Tab
                {
                    Title = Localization.GetString("SavedGames"),
                    Items =
                    {
                        new ShellContent { Content = savedGamesPage }
                    }
                },
                new Tab
                {
                    Title = Localization.GetString("Rules"),
                    Items =
                    {
                        new ShellContent { Content = rulesPage }
                    }
                },
                new Tab
                {
                    Title = Localization.GetString("Settings"),
                    Items =
                    {
                        new ShellContent { Content = settingsPage }
                    }
                }
            }
        });
    }
}

using WizardScoreboard.Pages;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard;

public partial class AppShell : Shell
{
    public AppShell(SettingsPage settingsPage, RulesPage rulesPage, HighscorePage highscorePage, SavedGamesPage savedGamesPage, ScoreBoardPage scoreBoardPage, GroupsPage groupsPage, AboutPage aboutPage)
    {
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
        Routing.RegisterRoute(nameof(RulesPage), typeof(RulesPage));
        Routing.RegisterRoute(nameof(HighscorePage), typeof(HighscorePage));
        Routing.RegisterRoute(nameof(SavedGamesPage), typeof(SavedGamesPage));
        Routing.RegisterRoute(nameof(ScoreBoardPage), typeof(ScoreBoardPage));
        Routing.RegisterRoute(nameof(GroupsPage), typeof(GroupsPage));

        Items.Add(new TabBar
        {
            Items =
            {
                new Tab
                {
                    Title = Localization.GetString("Scoreboard"),
                    Items =
                    {
                        new ShellContent { Route = nameof(ScoreBoardPage), Content = scoreBoardPage }
                    }
                },
                new Tab
                {
                    Title = Localization.GetString("Highscore"),
                    Items =
                    {
                        new ShellContent { Route = nameof(HighscorePage), Content = highscorePage }
                    }
                },
                new Tab
                {
                    Title = Localization.GetString("SavedGames"),
                    Items =
                    {
                        new ShellContent { Route = nameof(SavedGamesPage), Content = savedGamesPage }
                    }
                },
                new Tab
                {
                    Title = Localization.GetString("Rules"),
                    Items =
                    {
                        new ShellContent { Route = nameof(RulesPage), Content = rulesPage }
                    }
                },
                new Tab
                {
                    Title = Localization.GetString("Groups"),
                    Items =
                    {
                        new ShellContent { Route = nameof(GroupsPage), Content = groupsPage }
                    }
                }
            }
        });

        // Android's bottom navigation bar only shows up to 5 tabs, so the less-frequently
        // used pages live in the flyout menu instead of the tab bar.
        FlyoutBehavior = FlyoutBehavior.Flyout;

        Items.Add(new ShellContent
        {
            Title = Localization.GetString("Settings"),
            Route = nameof(SettingsPage),
            Content = settingsPage
        });

        Items.Add(new ShellContent
        {
            Title = Localization.GetString("About"),
            Route = nameof(AboutPage),
            Content = aboutPage
        });
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        ApplyTabBarTextStyle();
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        // Tab items can be (re)created while navigating, so re-apply the bold styling.
        ApplyTabBarTextStyle();
    }

    // Applies bold (or normal) styling to the native tab bar text, following the
    // AppSettings.BoldAllText preference. The platform-specific work is done in the
    // partial implementations under the Platforms folder.
    public void ApplyTabBarTextStyle() => ApplyTabBarTextStylePlatform(AppSettings.BoldAllText);

    partial void ApplyTabBarTextStylePlatform(bool bold);
}

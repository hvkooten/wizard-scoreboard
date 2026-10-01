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

        FlyoutBehavior = FlyoutBehavior.Flyout;
        // Light-grey flyout backdrop so the fiery header/logo stands out.
        FlyoutBackgroundColor = AppColors.Flyout;

        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            // On Android every page lives in the flyout menu (no bottom tab bar).
            AddFlyoutItem("Scoreboard", nameof(ScoreBoardPage), scoreBoardPage);
            AddFlyoutItem("Highscore", nameof(HighscorePage), highscorePage);
            AddFlyoutItem("SavedGames", nameof(SavedGamesPage), savedGamesPage);
            AddFlyoutItem("Rules", nameof(RulesPage), rulesPage);
            AddFlyoutItem("Groups", nameof(GroupsPage), groupsPage);
            AddFlyoutItem("Settings", nameof(SettingsPage), settingsPage);
            AddFlyoutItem("About", nameof(AboutPage), aboutPage);
        }
        else
        {
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
                    },
                    new Tab
                    {
                        Title = Localization.GetString("Settings"),
                        Items =
                        {
                            new ShellContent { Route = nameof(SettingsPage), Content = settingsPage }
                        }
                    },
                    new Tab
                    {
                        Title = Localization.GetString("About"),
                        Items =
                        {
                            new ShellContent { Route = nameof(AboutPage), Content = aboutPage }
                        }
                    }
                }
            });
        }
    }

    // Adds a page to the Shell flyout menu using a localized title and a fixed route.
    private void AddFlyoutItem(string localizationKey, string route, Page content) =>
        Items.Add(new ShellContent
        {
            Title = Localization.GetString(localizationKey),
            Route = route,
            Content = content
        });

    // Selects the page with the given route as the current Shell page (tab or flyout item).
    public void SelectPage(string route)
    {
        foreach (var item in Items)
        {
            foreach (var section in item.Items)
            {
                var content = section.Items.FirstOrDefault(c => c.Route == route);
                if (content != null)
                {
                    section.CurrentItem = content;
                    item.CurrentItem = section;
                    CurrentItem = item;
                    return;
                }
            }
        }
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

        // The newly navigated-to page's content is built fresh and does not know the current
        // bold-text preference, so apply it directly to the new page's visual tree.
        App.ApplyBoldToVisualTree(CurrentPage, AppSettings.BoldAllText);
    }

    // Applies bold (or normal) styling to the native tab bar text, following the
    // AppSettings.BoldAllText preference. The platform-specific work is done in the
    // partial implementations under the Platforms folder.
    public void ApplyTabBarTextStyle() => ApplyTabBarTextStylePlatform(AppSettings.BoldAllText);

    partial void ApplyTabBarTextStylePlatform(bool bold);
}

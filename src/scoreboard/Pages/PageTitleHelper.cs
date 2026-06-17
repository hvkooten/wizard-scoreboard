using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using WizardScoreboard.Resources;

namespace WizardScoreboard.Pages;

internal static class PageTitleHelper
{
    public static void Apply(ContentPage page, string leftTitle)
    {
        page.Title = leftTitle;

        var isAndroid = DeviceInfo.Platform == DevicePlatform.Android;
        Shell.SetTabBarIsVisible(page, !isAndroid);

        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            },
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center,
            MinimumWidthRequest = 260
        };

        var titleRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12
        };

        var titleLabel = new Label
        {
            Text = leftTitle,
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            Shadow = new Shadow
            {
                Brush = Colors.White,
                Offset = new Point(0, 0),
                Radius = 6,
                Opacity = 0.95f
            },
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Start,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var appLabel = new Label
        {
            Text = "Wizards",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            Shadow = new Shadow
            {
                Brush = Colors.White,
                Offset = new Point(0, 0),
                Radius = 6,
                Opacity = 0.95f
            },
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.End,
            HorizontalOptions = LayoutOptions.End
        };

        titleRow.Add(titleLabel, 0, 0);
        titleRow.Add(appLabel, 1, 0);
        grid.Add(titleRow, 0, 0);

        if (isAndroid)
        {
            var navRow = new HorizontalStackLayout
            {
                Spacing = 8,
                Padding = new Thickness(0, 8, 0, 0)
            };

            var activeRoute = page.GetType().Name;

            AddNavButton(Localization.GetString("Scoreboard"), nameof(ScoreBoardPage));
            AddNavButton(Localization.GetString("Highscore"), nameof(HighscorePage));
            AddNavButton(Localization.GetString("SavedGames"), nameof(SavedGamesPage));
            AddNavButton(Localization.GetString("Rules"), nameof(RulesPage));
            AddNavButton(Localization.GetString("Settings"), nameof(SettingsPage));

            var navScroller = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                Content = navRow
            };
            grid.Add(navScroller, 0, 1);

            void AddNavButton(string text, string route)
            {
                var isActive = string.Equals(activeRoute, route, StringComparison.Ordinal);

                var button = new Button
                {
                    Text = text,
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    CornerRadius = 10,
                    Padding = new Thickness(12, 8),
                    BackgroundColor = isActive ? Color.FromArgb("#d7e9ff") : Color.FromArgb("#edf4ff"),
                    TextColor = Color.FromArgb("#163a5f")
                };

                button.Clicked += async (_, _) => await Shell.Current.GoToAsync(route);
                navRow.Children.Add(button);
            }
        }

        void SyncTitleWidth()
        {
            if (page.Width > 0)
            {
                // Keep title view width in sync with window size so the right label stays anchored.
                grid.WidthRequest = Math.Max(260, page.Width - 48);
            }
        }

        page.SizeChanged += (_, _) => SyncTitleWidth();
        SyncTitleWidth();

        Shell.SetTitleView(page, grid);
    }
}
using System.Linq;
using Microsoft.Maui.Controls;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

public class SavedGamesPage : ContentPage
{
    private readonly IScoreService scoreService;
    private readonly VerticalStackLayout listLayout;

    public SavedGamesPage(IScoreService scoreService)
    {
        this.scoreService = scoreService;
        Title = Localization.GetString("SavedGames");

        listLayout = new VerticalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(12)
        };

        Content = new ScrollView
        {
            BackgroundColor = Colors.White,
            Content = listLayout
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BuildSavedGamesList();
    }

    private void BuildSavedGamesList()
    {
        listLayout.Children.Clear();

        var savedGames = scoreService.GetSavedGames().ToList();
        if (savedGames.Count == 0)
        {
            listLayout.Children.Add(new Label
            {
                Text = Localization.GetString("NoSavedGames"),
                FontSize = 16,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 16)
            });
            return;
        }

        foreach (var session in savedGames)
        {
            var savedDate = session.StartDate.ToLocalTime().ToString("g");
            var header = new Label
            {
                Text = $"{Localization.GetString("SavedOn")}: {savedDate}",
                FontAttributes = FontAttributes.Bold,
                FontSize = 14
            };

            var playersText = string.Join("\n", session.Players
                .OrderBy(p => p.Order)
                .Select(p => $"{p.Name}: {p.CurrentPoints}"));

            var playersLabel = new Label
            {
                Text = playersText,
                FontSize = 14
            };

            var openButton = new Button
            {
                Text = Localization.GetString("OpenGame")
            };

            var sessionId = session.Id;
            openButton.Clicked += async (s, e) =>
            {
                scoreService.SelectSavedGame(sessionId);
                await Shell.Current.GoToAsync($"//{nameof(ScoreBoardPage)}");
            };

            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#f8fbff"),
                Stroke = Color.FromArgb("#d6dce5"),
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(12),
                Content = new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        header,
                        playersLabel,
                        openButton
                    }
                }
            };

            listLayout.Children.Add(card);
        }
    }
}

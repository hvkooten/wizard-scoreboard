using WizardScoreboard.Models;
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
        PageTitleHelper.Apply(this, Localization.GetString("SavedGames"));

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

        // Rebuild when the global bold-text setting changes so the page updates immediately.
        App.GlobalTextStyleChanged += BuildSavedGamesList;
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

        var detailsLayouts = new List<VerticalStackLayout>();
        var toggleLabels = new List<Label>();

        void SetExpanded(int expandedIndex)
        {
            for (var i = 0; i < detailsLayouts.Count; i++)
            {
                var isExpanded = i == expandedIndex;
                detailsLayouts[i].IsVisible = isExpanded;
                toggleLabels[i].Text = isExpanded ? "[-]" : "[+]";
            }
        }

        for (var index = 0; index < savedGames.Count; index++)
        {
            var session = savedGames[index];
            var savedDate = session.StartDate.ToLocalTime().ToString("g");
            var header = new Label
            {
                Text = $"{Localization.GetString("SavedOn")}: {savedDate}",
                FontAttributes = FontAttributes.Bold,
                FontSize = 14
            };

            var subHeader = new Label
            {
                Text = $"{Localization.GetString("Players")}: {session.Players.Count}",
                FontSize = 12,
                TextColor = Colors.Gray
            };

            var toggleLabel = new Label
            {
                Text = "[+]",
                FontSize = 12,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.End,
                WidthRequest = 36
            };

            var deleteButton = UiFactory.CreateDeleteButton(36, 32, 14);

            var sessionScores = SessionScoreCalculator.CalculateSessionScores(session);

            var playersText = string.Join("\n", session.Players
                .OrderBy(p => p.Order)
                .Select(p => $"{p.Name}: {sessionScores.GetValueOrDefault(p.Id, 0)}"));

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

            deleteButton.Clicked += async (s, e) =>
            {
                var confirm = await DisplayAlertAsync(
                    Localization.GetString("DeleteSavedGameConfirmTitle"),
                    string.Format(Localization.GetString("DeleteSavedGameConfirmMessage"), savedDate),
                    Localization.GetString("Yes"),
                    Localization.GetString("No"));

                if (!confirm)
                {
                    return;
                }

                scoreService.DeleteSavedGame(sessionId);
                BuildSavedGamesList();
            };

            var detailsLayout = new VerticalStackLayout
            {
                Spacing = 8,
                IsVisible = false,
                Children =
                {
                    playersLabel,
                    openButton
                }
            };

            var headerRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var headerStack = new VerticalStackLayout
            {
                Spacing = 2,
                Children = { header, subHeader }
            };

            headerRow.Add(headerStack, 0, 0);
            headerRow.Add(deleteButton, 1, 0);
            headerRow.Add(toggleLabel, 2, 0);

            var tap = new TapGestureRecognizer();
            var capturedIndex = index;
            tap.Tapped += (s, e) =>
            {
                var shouldExpand = !detailsLayouts[capturedIndex].IsVisible;
                SetExpanded(shouldExpand ? capturedIndex : -1);
            };
            headerRow.GestureRecognizers.Add(tap);

            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#f8fbff"),
                Stroke = AppColors.Border,
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(12),
                Content = new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        headerRow,
                        detailsLayout
                    }
                }
            };

            detailsLayouts.Add(detailsLayout);
            toggleLabels.Add(toggleLabel);
            listLayout.Children.Add(card);
        }

        // Newest saved game (first item) is expanded by default.
        SetExpanded(0);
    }
}

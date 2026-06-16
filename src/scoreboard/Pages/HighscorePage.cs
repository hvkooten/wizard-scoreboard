using Microsoft.Maui.Controls;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

public class HighscorePage : ContentPage
{
    private readonly IHighscoreService highscoreService;
    private readonly IGroupService groupService;

    private readonly CollectionView listView;

    public HighscorePage(IHighscoreService highscoreService, IGroupService groupService)
    {
        Title = Localization.GetString("Highscore");

        this.highscoreService = highscoreService;
        this.groupService = groupService;

        listView = new CollectionView();

        Content = new StackLayout
        {
            Padding = 20,
            Children =
            {
                new Label
                {
                    Text = "Wizard",
                    FontSize = 11,
                    TextColor = Colors.Gray,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8)
                },
                listView
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshHighscores();
    }

    public void RefreshHighscores()
    {
        highscoreService.UpdateHighscores(groupService.GetGroups());
        listView.ItemsSource = highscoreService
            .GetHighscores()
            .Select(p => string.Format(Localization.GetString("HighscoreEntryTemplate"), p.Name, p.Wins, p.GamesPlayed, p.HighestScore))
            .ToList();
    }
}

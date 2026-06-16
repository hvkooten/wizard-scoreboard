using Microsoft.Maui.Controls;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

public class HighscorePage : ContentPage
{
    private readonly IHighscoreService highscoreService;
    private readonly IGroupService groupService;

    public HighscorePage(IHighscoreService highscoreService, IGroupService groupService)
    {
        Title = Localization.GetString("Highscore");

        this.highscoreService = highscoreService;
        this.groupService = groupService;

        var refreshButton = new Button { Text = Localization.GetString("Refresh") };
        var listView = new CollectionView();

        refreshButton.Clicked += (s, e) =>
        {
            highscoreService.UpdateHighscores(groupService.GetGroups());
            listView.ItemsSource = highscoreService
                .GetHighscores()
                .Select(p => string.Format(Localization.GetString("HighscoreEntryTemplate"), p.Name, p.Wins, p.GamesPlayed, p.HighestScore))
                .ToList();
        };

        Content = new StackLayout
        {
            Padding = 20,
            Children =
            {
                refreshButton,
                listView
            }
        };

        refreshButton.SendClicked();
    }
}

using Microsoft.Maui.Controls;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

public class HighscorePage : ContentPage
{
    private readonly HighscoreService highscoreService;
    private readonly IGroupService groupService;

    public HighscorePage()
    {
        Title = Localization.GetString("Highscore");

        highscoreService = new HighscoreService();
        groupService = new GroupService();

        var refreshButton = new Button { Text = "Refresh" };
        var listView = new ListView();

        refreshButton.Clicked += (s, e) =>
        {
            highscoreService.UpdateHighscores(groupService.GetGroups());
            listView.ItemsSource = highscoreService.GetHighscores().Select(p => $"{p.Name}: wins={p.Wins}, best={p.HighestScore}").ToList();
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

using Microsoft.Maui.Controls;
using WizardScoreboard.Models;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

public class ScoreBoardPage : ContentPage
{
    private readonly GroupService groupService;
    private readonly ScoreService scoreService;
    private ScoreSession? currentSession;
    private Label statusLabel;
    private StackLayout playersLayout;
    private Button startButton;
    private Button endButton;
    private Button nextRoundButton;

    public ScoreBoardPage()
    {
        Title = Localization.GetString("Scoreboard");

        groupService = new GroupService();
        scoreService = new ScoreService();

        statusLabel = new Label { Text = "No active game.", FontAttributes = FontAttributes.Bold };
        playersLayout = new StackLayout { Spacing = 6 };

        startButton = new Button { Text = "Start Game" };
        endButton = new Button { Text = "End Game" };
        nextRoundButton = new Button { Text = "Next Round" };

        startButton.Clicked += async (s, e) => await StartGameAsync();
        endButton.Clicked += (s, e) => EndGame();
        nextRoundButton.Clicked += async (s, e) => await StartNextRoundAsync();

        Content = new ScrollView
        {
            Content = new StackLayout
            {
                Padding = 20,
                Spacing = 12,
                Children = { statusLabel, playersLayout, startButton, nextRoundButton, endButton }
            }
        };

        RefreshUI();
    }

    private void RefreshUI()
    {
        playersLayout.Children.Clear();

        if (currentSession != null)
        {
            statusLabel.Text = $"Game: round {currentSession.CurrentRound}/{currentSession.MaxRounds}, dealer {currentSession.Players[currentSession.CurrentRound % currentSession.Players.Count].Name}";
            playersLayout.Children.Add(new Label { Text = "Players:" });
            foreach (var player in currentSession.Players)
            {
                playersLayout.Children.Add(new Label { Text = $"{player.Order + 1}. {player.Name}: {player.CurrentPoints} pts, wins {player.Wins}" });
            }
            BackgroundColor = GetTrumpColor(currentSession.Trump);
        }
        else
        {
            statusLabel.Text = "No active game.";
            BackgroundColor = Colors.White;
        }
    }

    private async Task StartGameAsync()
    {
        var group = groupService.GetGroups().FirstOrDefault();
        if (group == null)
        {
            await DisplayAlert("Error", "No groups available. Create group first in settings.", "OK");
            return;
        }

        currentSession = scoreService.StartGame(group);
        await DisplayRoundPopupAsync();
        RefreshUI();
    }

    private void EndGame()
    {
        if (currentSession != null)
        {
            scoreService.EndGame(currentSession);
            currentSession = null;
            RefreshUI();
        }
    }

    private async Task StartNextRoundAsync()
    {
        if (currentSession == null || !currentSession.IsActive)
        {
            await DisplayAlert("Info", "No active game.", "OK");
            return;
        }

        if (currentSession.CurrentRound >= currentSession.MaxRounds)
        {
            await DisplayAlert("Info", "Game already completed.", "OK");
            return;
        }

        await DisplayRoundPopupAsync();
        RefreshUI();
    }

    private async Task DisplayRoundPopupAsync()
    {
        if (currentSession == null)
            return;

        var trumpOptions = new[] { "None", "Hearts", "Diamonds", "Clubs", "Spades" };
        string trumpSelection = "None";
        int predictedAmount = 0;

        var population = new StackLayout { Spacing = 10, Padding = 12 };
        var numberEntry = new Entry { Keyboard = Keyboard.Numeric, Placeholder = "Predicted tricks (0..round)", Text = "0" };
        var trumpPicker = new Picker { Title = "Trump" };
        foreach (var t in trumpOptions) trumpPicker.Items.Add(t);
        trumpPicker.SelectedIndex = 0;

        var modal = new ContentPage { Content = population };
        population.Children.Add(numberEntry);
        population.Children.Add(trumpPicker);

        var doneButton = new Button { Text = "OK" };
        population.Children.Add(doneButton);

        var tcs = new TaskCompletionSource<bool>();

        doneButton.Clicked += (s, e) =>
        {
            if (!int.TryParse(numberEntry.Text, out predictedAmount))
            {
                predictedAmount = 0;
            }
            trumpSelection = trumpPicker.SelectedItem?.ToString() ?? "None";
            tcs.SetResult(true);
        };

        await Navigation.PushModalAsync(modal);
        await tcs.Task;
        await Navigation.PopModalAsync();

        if (!Enum.TryParse<TrumpSuit>(trumpSelection, out var trump))
        {
            trump = TrumpSuit.None;
        }

        if (predictedAmount < 0 || predictedAmount > currentSession.MaxRounds)
        {
            await DisplayAlert("Error", "Prediction must be inside range.", "OK");
            return;
        }

        var bids = currentSession.Players.ToDictionary(p => p.Id, p => predictedAmount);
        try
        {
            var round = scoreService.StartRound(currentSession, trump, bids);
            // placeholder real logic: mark each with actual=bid
            scoreService.FinishRound(currentSession, bids);
            RefreshUI();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private Color GetTrumpColor(TrumpSuit trump)
    {
        return trump switch
        {
            TrumpSuit.Hearts => Colors.Red.WithAlpha(0.2f),
            TrumpSuit.Diamonds => Colors.Orange.WithAlpha(0.2f),
            TrumpSuit.Clubs => Colors.Green.WithAlpha(0.2f),
            TrumpSuit.Spades => Colors.Blue.WithAlpha(0.2f),
            _ => Colors.White
        };
    }
}

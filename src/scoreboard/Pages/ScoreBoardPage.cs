using Microsoft.Maui.Controls;
using WizardScoreboard.Models;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

public class ScoreBoardPage : ContentPage
{
    private readonly IGroupService groupService;
    private readonly IHighscoreService highscoreService;
    private readonly IScoreService scoreService;
    private readonly ITrumpPaletteService trumpPaletteService;
    private ScoreSession? currentSession;

    // Live-updated UI elements
    private Label statusLabel;
    private Grid scoreGrid;
    private Grid headerGrid;
    private Grid footerGrid;
    private Button startButton;
    private Button pauseButton;
    private Button endButton;
    private Button nextRoundButton;
    private ScrollView scoreboardScrollView;

    // Palette for header colours
    private static readonly Color HeaderBg   = Color.FromArgb("#1a3a5c");
    private static readonly Color HeaderFg   = Colors.White;
    private static readonly Color RowEven    = Color.FromArgb("#f0f4f8");
    private static readonly Color RowOdd     = Colors.White;
    private static readonly Color WinBg      = Color.FromArgb("#c8f7c5");
    private static readonly Color LoseBg     = Color.FromArgb("#fde8e8");
    private static readonly Color WinFg      = Color.FromArgb("#1a6b2a");
    private static readonly Color LoseFg     = Color.FromArgb("#a32020");
    private static readonly Color TotalBg    = Color.FromArgb("#ddeeff");

    public ScoreBoardPage(IGroupService groupService, IHighscoreService highscoreService, IScoreService scoreService, ITrumpPaletteService trumpPaletteService)
    {
        PageTitleHelper.Apply(this, Localization.GetString("Scoreboard"));

        this.groupService    = groupService;
        this.highscoreService = highscoreService;
        this.scoreService    = scoreService;
        this.trumpPaletteService = trumpPaletteService;

        statusLabel = new Label
        {
            FontAttributes = FontAttributes.Bold,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };

        scoreGrid = new Grid();
        headerGrid = new Grid();
        footerGrid = new Grid();

        startButton     = new Button { Text = Localization.GetString("StartGame") };
        pauseButton     = new Button { Text = Localization.GetString("PauseGame") };
        endButton       = new Button { Text = Localization.GetString("EndGame") };
        nextRoundButton = new Button { Text = Localization.GetString("NextRound") };

        startButton.Clicked     += async (s, e) => await StartGameAsync();
        pauseButton.Clicked     += async (s, e) => await TogglePauseAsync();
        endButton.Clicked       += async (s, e) => await EndGameAsync();
        nextRoundButton.Clicked += async (s, e) => await StartNextRoundAsync();

        var buttonRow = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { startButton, nextRoundButton, pauseButton, endButton }
        };

        scoreboardScrollView = new ScrollView 
        { 
            BackgroundColor = Colors.White,
            Padding = new Thickness(12)
        };
        scoreboardScrollView.Content = scoreGrid;

        // Grid with fixed top controls, fixed table header, scrollable rounds, fixed footer
        var mainGrid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto },
            }
        };

        var headerStack = new StackLayout
        {
            Padding = 12,
            Spacing = 8,
            BackgroundColor = Colors.White,
            Children = { statusLabel, buttonRow }
        };

        mainGrid.Add(headerStack, 0, 0);
        mainGrid.Add(headerGrid, 0, 1);
        mainGrid.Add(scoreboardScrollView, 0, 2);
        mainGrid.Add(footerGrid, 0, 3);

        Content = mainGrid;

        RefreshUI();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Ensure the last used group is automatically loaded
        if (currentSession == null)
        {
            currentSession = scoreService.GetCurrentSession();

            if (currentSession != null)
            {
                groupService.SetSelectedGroup(currentSession.GroupId);
            }

            var selectedGroup = groupService.GetSelectedGroup();
            if (selectedGroup != null)
            {
                groupService.SetSelectedGroup(selectedGroup.Id);
            }
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        headerGrid.Children.Clear();
        headerGrid.RowDefinitions.Clear();
        headerGrid.ColumnDefinitions.Clear();
        scoreGrid.Children.Clear();
        scoreGrid.RowDefinitions.Clear();
        scoreGrid.ColumnDefinitions.Clear();
        footerGrid.Children.Clear();
        footerGrid.RowDefinitions.Clear();
        footerGrid.ColumnDefinitions.Clear();

        var hasAvailableGroup = groupService.GetSelectedGroup() != null || groupService.GetGroups().Any();
        var hasSession = currentSession != null;
        var isActiveSession = currentSession?.IsActive == true;
        var isPausedSession = currentSession?.IsPaused == true;
        var hasRoundsRemaining = isActiveSession
            && !isPausedSession
            && currentSession!.CurrentRound < currentSession.MaxRounds;

        startButton.IsEnabled = hasAvailableGroup && (!hasSession || isPausedSession);
        pauseButton.IsEnabled = isActiveSession;
        pauseButton.Text = isPausedSession
            ? Localization.GetString("ResumeGame")
            : Localization.GetString("PauseGame");
        nextRoundButton.IsEnabled = hasRoundsRemaining;
        endButton.IsEnabled = hasSession;

        // Show only actions that are currently available.
        startButton.IsVisible = startButton.IsEnabled;
        pauseButton.IsVisible = pauseButton.IsEnabled;
        nextRoundButton.IsVisible = nextRoundButton.IsEnabled;
        endButton.IsVisible = endButton.IsEnabled;

        if (currentSession == null)
        {
            statusLabel.Text = Localization.GetString("NoActiveGame");
            BackgroundColor = Colors.White;

            // Show players from selected group if available
            var selectedGroup = groupService.GetSelectedGroup();
            if (selectedGroup != null && selectedGroup.Players.Any())
            {
                var groupPlayers = selectedGroup.Players.OrderBy(p => p.Order).ToList();

                // Set up column definition
                scoreGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                // Header: Group name
                scoreGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var headerLbl = new Label
                {
                    Text = selectedGroup.Name,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 16,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Padding = new Thickness(12, 8)
                };
                scoreGrid.Add(headerLbl, 0, 0);

                // Players list
                for (int i = 0; i < groupPlayers.Count; i++)
                {
                    scoreGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    var playerLbl = new Label
                    {
                        Text = groupPlayers[i].Name,
                        FontSize = 14,
                        HorizontalTextAlignment = TextAlignment.Center,
                        Padding = new Thickness(12, 12),
                        BackgroundColor = (i % 2 == 0) ? RowEven : RowOdd
                    };
                    scoreGrid.Add(playerLbl, 0, i + 1);
                }
            }

            return;
        }

        var dealerIndex = (currentSession.CurrentRound) % currentSession.Players.Count;
        var dealer = currentSession.Players[dealerIndex];
        // Show the NEXT round to be played (or last if game over).
        var displayRound = currentSession.IsActive
            ? currentSession.CurrentRound + 1
            : currentSession.CurrentRound;
        statusLabel.Text = string.Format(
            Localization.GetString("ScoreStatusTemplate"),
            displayRound,
            currentSession.MaxRounds,
            dealer.Name);
        if (currentSession.IsPaused)
        {
            statusLabel.Text = $"{statusLabel.Text} ({Localization.GetString("GamePausedStatus")})";
        }

        BackgroundColor = GetTrumpColor(currentSession.Trump);

        var players = currentSession.Players.OrderBy(p => p.Order).ToList();

        // ── Column definitions ────────────────────────────────────────────
        // Col 0 = Rnd, Col 1 = Dealer, Col 2..N = players
        foreach (var grid in new[] { headerGrid, scoreGrid, footerGrid })
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = 36 });  // Rnd
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = 36 });  // Dealer / trump
            foreach (var _ in players)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }

        // ── Header row ────────────────────────────────────────────────────
        headerGrid.RowDefinitions.Add(new RowDefinition { Height = 36 });
        AddHeaderCell(headerGrid, Localization.GetString("RoundHeader"), 0, 0);
        AddHeaderCell(headerGrid, TrumpHeaderSymbol(), 0, 1);  // column label
        for (var c = 0; c < players.Count; c++)
        {
            AddHeaderCell(headerGrid, players[c].Name, 0, 2 + c,
                isDealer: players[c].Id == dealer.Id && currentSession.CurrentRound > 0);
        }

        // ── One row per round (all rounds, completed + future) ────────────
        var runningTotals = players.ToDictionary(p => p.Id, _ => 0);

        for (var roundNum = 1; roundNum <= currentSession.MaxRounds; roundNum++)
        {
            var round = currentSession.Rounds.FirstOrDefault(r => r.RoundNumber == roundNum);
            var isCurrentRound = roundNum == currentSession.CurrentRound + 1 && currentSession.IsActive;
            var isEven = (roundNum - 1) % 2 == 0;
            var rowBg = isCurrentRound
                ? Color.FromArgb("#fff8e1")
                : (isEven ? RowEven : RowOdd);
            var gridRow = scoreGrid.RowDefinitions.Count;
            scoreGrid.RowDefinitions.Add(new RowDefinition { Height = 22 });  // bid row
            scoreGrid.RowDefinitions.Add(new RowDefinition { Height = 28 });  // total row

            // Round number (spans 2 rows)
            var rndLabel = MakeLabel(roundNum.ToString(),
                bold: isCurrentRound, fontSize: 13, center: true, bg: rowBg);
            scoreGrid.Add(rndLabel, 0, gridRow);
            Grid.SetRowSpan(rndLabel, 2);

            // Trump symbol (spans 2 rows) — shows dot for future rounds
            string trumpText = round != null ? TrumpSymbol(round.Trump) : FutureRoundTrumpSymbol();
            Color trumpFg    = round != null ? TrumpColor(round.Trump) : Colors.LightGray;
            var trumpLbl = MakeLabel(trumpText, bold: true, fontSize: 16, center: true, bg: rowBg, fg: trumpFg);
            scoreGrid.Add(trumpLbl, 1, gridRow);
            Grid.SetRowSpan(trumpLbl, 2);

            for (var c = 0; c < players.Count; c++)
            {
                var pid = players[c].Id;

                if (round == null)
                {
                    // Future round — empty cells
                    scoreGrid.Add(MakeLabel("", bg: rowBg), 2 + c, gridRow);
                    scoreGrid.Add(MakeLabel("", bg: rowBg), 2 + c, gridRow + 1);
                    continue;
                }

                var bid    = round.BidByPlayer.GetValueOrDefault(pid, -1);
                var actual = round.ActualByPlayer.GetValueOrDefault(pid, -1);

                Color cellBg;
                Color bidFg = Colors.Gray;

                if (actual >= 0)
                {
                    var delta = bid == actual ? 2 + actual : -(Math.Abs(bid - actual));
                    runningTotals[pid] += delta;
                    cellBg = delta >= 0 ? WinBg : LoseBg;
                    bidFg  = delta >= 0 ? WinFg : LoseFg;
                }
                else
                {
                    cellBg = rowBg;
                }

                // Top row: show bid and actual wins together.
                var bidText = bid >= 0
                    ? (actual >= 0 ? $"{bid}/{actual}" : $"{bid}/?")
                    : "-";
                var bidLabel = MakeLabel(bidText, fontSize: 11, fg: bidFg, bg: cellBg,
                    padding: new Thickness(3, 1, 0, 0));
                scoreGrid.Add(bidLabel, 2 + c, gridRow);

                // Total row: large, centred
                var totalText = actual >= 0 ? runningTotals[pid].ToString() : "";
                var totalFg = runningTotals[pid] >= 0 ? WinFg : LoseFg;
                var totalLabel = MakeLabel(totalText, bold: true, fontSize: 15,
                    center: true, bg: cellBg, fg: actual >= 0 ? totalFg : Colors.Black);
                scoreGrid.Add(totalLabel, 2 + c, gridRow + 1);
            }
        }

        // ── Total row (always visible) ────────────────────────────────────
        footerGrid.RowDefinitions.Add(new RowDefinition { Height = 32 });

        var totHdr = MakeLabel(Localization.GetString("TotalHeader"),
            bold: true, fontSize: 13, center: true, bg: TotalBg);
        footerGrid.Add(totHdr, 0, 0);
        Grid.SetColumnSpan(totHdr, 2);

        for (var c = 0; c < players.Count; c++)
        {
            var total = runningTotals[players[c].Id];
            AddCell(footerGrid, total.ToString(), 0, 2 + c,
                bold: true, center: true,
                color: total >= 0 ? WinFg : LoseFg, bg: TotalBg);
        }

        // Auto-scroll to center the latest played round only when content overflows.
        var sessionForScroll = currentSession;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(100);

            if (sessionForScroll == null || !sessionForScroll.IsActive)
            {
                return;
            }

            var viewportHeight = scoreboardScrollView.Height;

            if (viewportHeight <= 0 || sessionForScroll.CurrentRound <= 0)
            {
                return;
            }

            const double roundHeight = 50; // 22 + 28
            var contentHeight = sessionForScroll.MaxRounds * roundHeight;
            if (contentHeight <= viewportHeight)
            {
                return;
            }

            var lastPlayedRound = sessionForScroll.CurrentRound;
            var roundCenterY = ((lastPlayedRound - 1) * roundHeight) + (roundHeight / 2);
            var desiredY = roundCenterY - (viewportHeight / 2);
            var maxScrollY = Math.Max(0, contentHeight - viewportHeight);
            var targetY = Math.Max(0, Math.Min(desiredY, maxScrollY));

            await scoreboardScrollView.ScrollToAsync(0, targetY, false);
        });
    }

    // ── Cell helpers ──────────────────────────────────────────────────────

    private static Border MakeLabel(string text, bool bold = false, double fontSize = 13,
        bool center = false, Color? bg = null, Color? fg = null,
        Thickness? padding = null)
    {
        var lbl = new Label
        {
            Text = text,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            FontSize = fontSize,
            HorizontalTextAlignment = center ? TextAlignment.Center : TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
            Padding = padding ?? new Thickness(2)
        };
        if (fg is Color f) lbl.TextColor = f;

        return new Border
        {
            BackgroundColor = bg ?? Colors.Transparent,
            Stroke = Color.FromArgb("#d6dce5"),
            StrokeThickness = 0.75,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 0 },
            Padding = 0,
            Content = lbl
        };
    }

    private static void AddHeaderCell(Grid grid, string text, int row, int col, bool isDealer = false)
    {
        var lbl = new Label
        {
            Text = text,
            FontAttributes = FontAttributes.Bold,
            FontSize = isDealer ? 14 : 13,
            TextColor = isDealer ? Colors.Black : HeaderFg,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            Padding = new Thickness(4, 2)
        };

        var border = new Border
        {
            BackgroundColor = isDealer ? Color.FromArgb("#ffd54f") : HeaderBg,
            Stroke = Color.FromArgb("#8aa0b8"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 0 },
            Padding = 0,
            Content = lbl
        };

        grid.Add(border, col, row);
    }

    private static void AddCell(Grid grid, string text, int row, int col,
        bool bold = false, bool center = false, Color? color = null, Color? bg = null, bool small = false)
    {
        var label = new Label
        {
            Text = text,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            FontSize = small ? 11 : 13,
            HorizontalTextAlignment = center ? TextAlignment.Center : TextAlignment.Start,
            VerticalTextAlignment = TextAlignment.Center,
            Padding = new Thickness(4, 2)
        };
        if (color is Color c) label.TextColor = c;

        var border = new Border
        {
            BackgroundColor = bg ?? Colors.Transparent,
            Stroke = Color.FromArgb("#d6dce5"),
            StrokeThickness = 0.75,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 0 },
            Padding = 0,
            Content = label
        };

        grid.Add(border, col, row);
    }

    private string TrumpHeaderSymbol()
    {
        return trumpPaletteService.GetMode() == TrumpPaletteMode.FourColors ? "●" : "🃏";
    }

    private string FutureRoundTrumpSymbol()
    {
        return trumpPaletteService.GetMode() == TrumpPaletteMode.FourColors ? "○" : "·";
    }

    private string TrumpSymbol(TrumpSuit trump)
    {
        if (trumpPaletteService.GetMode() == TrumpPaletteMode.FourColors)
        {
            return trump == TrumpSuit.None ? "○" : "●";
        }

        return trump switch
        {
            TrumpSuit.Hearts   => "♥",
            TrumpSuit.Diamonds => "♦",
            TrumpSuit.Clubs    => "♣",
            TrumpSuit.Spades   => "♠",
            _                  => "—"
        };
    }

    private Color TrumpColor(TrumpSuit trump) => trump switch
    {
        TrumpSuit.Hearts   => Colors.Red,
        TrumpSuit.Diamonds => trumpPaletteService.GetMode() == TrumpPaletteMode.FourColors
            ? Colors.Goldenrod
            : Color.FromArgb("#e05000"),
        TrumpSuit.Clubs    => Colors.DarkGreen,
        TrumpSuit.Spades   => Colors.DarkBlue,
        _                  => Colors.Gray
    };

    private async Task StartGameAsync()
    {
        var group = groupService.GetSelectedGroup() ?? groupService.GetGroups().FirstOrDefault();
        if (group == null)
        {
            await DisplayAlertAsync(Localization.GetString("ErrorTitle"), Localization.GetString("NoGroupsAvailable"), Localization.GetString("Ok"));
            return;
        }

        groupService.SetSelectedGroup(group.Id);

        // The player-selection modal lets the user switch groups or jump to group creation,
        // so keep re-showing it until the user starts a game or cancels.
        while (true)
        {
            var result = await SelectPlayersAsync(group);

            if (result.CreateNewGroup)
            {
                await Shell.Current.GoToAsync($"//{nameof(GroupsPage)}");
                return;
            }

            if (result.SwitchGroup)
            {
                group = groupService.GetSelectedGroup() ?? group;
                continue;
            }

            if (result.Players == null)
                return;

            currentSession = scoreService.StartGame(group, result.Players);
            await DisplayRoundPopupAsync();
            RefreshUI();
            return;
        }
    }

    private sealed record PlayerSelectionResult(List<Player>? Players, bool SwitchGroup, bool CreateNewGroup);

    private async Task EndGameAsync()
    {
        if (currentSession == null)
            return;

        var session = currentSession;

        if (session.IsActive && session.CurrentRound < session.MaxRounds)
        {
            var confirm = await DisplayAlertAsync(
                Localization.GetString("ConfirmEndGameTitle"),
                Localization.GetString("ConfirmEndGameMessage"),
                Localization.GetString("Yes"),
                Localization.GetString("No"));

            if (!confirm)
                return;
        }

        scoreService.EndGame(session);
        SyncSessionStatsToGroup(session);
        highscoreService.UpdateHighscores(groupService.GetGroups());

        if (session.Rounds.Count > 0)
        {
            await ShowGameFinishedCelebrationAsync(session);
        }

        currentSession = null;
        RefreshUI();
    }

    private async Task<PlayerSelectionResult> SelectPlayersAsync(Group group)
    {
        var allPlayers = group.Players.OrderBy(p => p.Order).ToList();

        var prefKey = $"player_sel_v1_{group.Id}";
        var savedRaw = Preferences.Default.Get(prefKey, string.Empty);
        var savedIds = savedRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToHashSet();

        var selected = new HashSet<Guid>(
            savedIds.Count >= 3 && savedIds.All(id => allPlayers.Any(p => p.Id == id))
                ? savedIds
                : allPlayers.Select(p => p.Id));

        var toggleButtons = new Dictionary<Guid, Button>();

        // When the group's bid-total rule follows the player count, the start round tracks the
        // number of selected players (optionally doubled) and resets to it on every player change
        // (a manual +/- override via the buttons below lasts only until the next player change).
        var followsPlayerCount = group.BidTotalRuleStartRound is < 0 or > 13;
        var followsDoublePlayerCount = group.BidTotalRuleStartRound == Group.DoublePlayerCountRule;
        int FollowValue() => followsDoublePlayerCount ? selected.Count * 2 : selected.Count;
        var bidRuleValue = followsPlayerCount ? Math.Min(FollowValue(), 13) : group.BidTotalRuleStartRound;
        Action? syncBidRuleWithPlayers = null;
        var countLabel = new Label
        {
            FontSize = 14,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var startBtn = new Button { Text = Localization.GetString("StartGame") };

        void UpdateUI()
        {
            countLabel.Text = string.Format(Localization.GetString("SelectedPlayersLabel"), selected.Count);
            startBtn.IsEnabled = selected.Count >= 3;
        }

        void ApplyStyle(Button btn, bool isOn)
        {
            btn.BackgroundColor = isOn ? Color.FromArgb("#1a3a5c") : Color.FromArgb("#e0e8f0");
            btn.TextColor = isOn ? Colors.White : Color.FromArgb("#1a3a5c");
        }

        var layout = new StackLayout { Spacing = 10, Padding = new Thickness(16, 16) };
        layout.Children.Add(new Label
        {
            Text = Localization.GetString("SelectPlayersTitle"),
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var tcs = new TaskCompletionSource<PlayerSelectionResult>();

        // Group selector so the user can switch to another saved group without leaving the dialog.
        var allGroups = groupService.GetGroups().OrderBy(g => g.CreatedAt).ToList();
        var groupPicker = new Picker
        {
            Title = Localization.GetString("SelectGroup"),
            HorizontalOptions = LayoutOptions.Fill
        };
        foreach (var g in allGroups)
        {
            groupPicker.Items.Add(g.Name);
        }
        groupPicker.SelectedIndex = allGroups.FindIndex(g => g.Id == group.Id);
        groupPicker.SelectedIndexChanged += (s, e) =>
        {
            if (groupPicker.SelectedIndex < 0 || groupPicker.SelectedIndex >= allGroups.Count)
                return;

            var chosen = allGroups[groupPicker.SelectedIndex];
            if (chosen.Id == group.Id)
                return;

            groupService.SetSelectedGroup(chosen.Id);
            tcs.TrySetResult(new PlayerSelectionResult(null, SwitchGroup: true, CreateNewGroup: false));
        };

        var newGroupBtn = new Button
        {
            Text = Localization.GetString("NewGroup"),
            WidthRequest = 52,
            HeightRequest = 44,
            Padding = new Thickness(0),
            FontSize = 18
        };
        newGroupBtn.Clicked += (s, e) =>
            tcs.TrySetResult(new PlayerSelectionResult(null, SwitchGroup: false, CreateNewGroup: true));

        layout.Children.Add(new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                groupPicker,
                newGroupBtn
            }
        });

        foreach (var player in allPlayers)
        {
            var btn = new Button
            {
                Text = player.Name,
                HeightRequest = 44,
                CornerRadius = 8,
                FontSize = 16
            };
            ApplyStyle(btn, selected.Contains(player.Id));
            var capturedId = player.Id;
            btn.Clicked += (s, e) =>
            {
                if (selected.Contains(capturedId))
                {
                    if (selected.Count > 3)
                    {
                        selected.Remove(capturedId);
                        ApplyStyle(btn, false);
                    }
                }
                else
                {
                    selected.Add(capturedId);
                    ApplyStyle(btn, true);
                }
                UpdateUI();
                syncBidRuleWithPlayers?.Invoke();
            };
            toggleButtons[player.Id] = btn;
            layout.Children.Add(btn);
        }

        layout.Children.Add(countLabel);

        // Bid total rule start round setting
        var bidRuleLabel = new Label
        {
            FontSize = 13,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 12, 0, 4)
        };
        
        var bidRuleMinusBtn = new Button
        {
            Text = "−",
            WidthRequest = 36,
            HeightRequest = 36,
            Padding = new Thickness(0),
            FontSize = 16,
            BackgroundColor = Color.FromArgb("#e0e8f0"),
            TextColor = Color.FromArgb("#1a3a5c")
        };
        var bidRulePlusBtn = new Button
        {
            Text = "+",
            WidthRequest = 36,
            HeightRequest = 36,
            Padding = new Thickness(0),
            FontSize = 16,
            BackgroundColor = Color.FromArgb("#1a3a5c"),
            TextColor = Colors.White
        };

        void UpdateBidRuleUI()
        {
            var bidRuleDisplay = bidRuleValue == 0
                ? Localization.GetString("Disabled")
                : bidRuleValue.ToString();
            bidRuleLabel.Text = $"{Localization.GetString("BidTotalRuleStartRound")}: {bidRuleDisplay}";
            bidRuleMinusBtn.IsEnabled = bidRuleValue > 0;
            bidRulePlusBtn.IsEnabled = bidRuleValue < 13;
        }

        bidRuleMinusBtn.Clicked += (s, e) =>
        {
            if (bidRuleValue > 0)
            {
                bidRuleValue--;
                UpdateBidRuleUI();
            }
        };

        bidRulePlusBtn.Clicked += (s, e) =>
        {
            if (bidRuleValue < 13)
            {
                bidRuleValue++;
                UpdateBidRuleUI();
            }
        };

        syncBidRuleWithPlayers = () =>
        {
            if (followsPlayerCount)
            {
                bidRuleValue = Math.Min(FollowValue(), 13);
                UpdateBidRuleUI();
            }
        };

        UpdateBidRuleUI();

        var bidRuleRow = new HorizontalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center,
            Children = { bidRuleMinusBtn, bidRuleLabel, bidRulePlusBtn }
        };
        layout.Children.Add(bidRuleRow);

        var cancelBtn = new Button { Text = Localization.GetString("Cancel") };
        var buttonRow = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { startBtn, cancelBtn }
        };
        layout.Children.Add(buttonRow);
        UpdateUI();

        var modal = new ContentPage { Content = new ScrollView { Content = layout } };

        startBtn.Clicked += (s, e) =>
        {
            Preferences.Default.Set(prefKey, string.Join(',', selected.Select(id => id.ToString())));
            // Preserve the follow-player-count mode in storage; otherwise persist the fixed round.
            group.BidTotalRuleStartRound = followsPlayerCount
                ? (followsDoublePlayerCount ? Group.DoublePlayerCountRule : Group.PlayerCountRule)
                : bidRuleValue;
            groupService.UpdateGroup(group);
            // Apply the effective value (including any manual override) to the in-memory group so the
            // game about to start uses it, without overwriting the persisted follow-player-count mode.
            group.BidTotalRuleStartRound = bidRuleValue;
            var result = allPlayers.Where(p => selected.Contains(p.Id)).ToList();
            tcs.TrySetResult(new PlayerSelectionResult(result, SwitchGroup: false, CreateNewGroup: false));
        };

        cancelBtn.Clicked += (s, e) =>
        {
            tcs.TrySetResult(new PlayerSelectionResult(null, SwitchGroup: false, CreateNewGroup: false));
        };

        modal.Disappearing += (s, e) => tcs.TrySetResult(new PlayerSelectionResult(null, SwitchGroup: false, CreateNewGroup: false));

        await Navigation.PushModalAsync(modal);
        var selectionResult = await tcs.Task;
        if (Navigation.ModalStack.Contains(modal))
            await Navigation.PopModalAsync();

        return selectionResult;
    }

    private async Task StartNextRoundAsync()
    {
        if (currentSession == null || !currentSession.IsActive)
        {
            await DisplayAlertAsync(Localization.GetString("InfoTitle"), Localization.GetString("NoActiveGame"), Localization.GetString("Ok"));
            return;
        }

        if (currentSession.IsPaused)
        {
            await DisplayAlertAsync(Localization.GetString("InfoTitle"), Localization.GetString("GamePausedStatus"), Localization.GetString("Ok"));
            return;
        }

        if (currentSession.CurrentRound >= currentSession.MaxRounds)
        {
            await DisplayAlertAsync(Localization.GetString("InfoTitle"), Localization.GetString("GameAlreadyCompleted"), Localization.GetString("Ok"));
            return;
        }

        await DisplayRoundPopupAsync();
        RefreshUI();
    }

    private async Task TogglePauseAsync()
    {
        if (currentSession == null || !currentSession.IsActive)
            return;

        try
        {
            if (currentSession.IsPaused)
            {
                scoreService.ResumeGame(currentSession);
            }
            else
            {
                scoreService.PauseGame(currentSession);
            }

            RefreshUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(Localization.GetString("ErrorTitle"), ex.Message, Localization.GetString("Ok"));
        }
    }

    private async Task DisplayRoundPopupAsync()
    {
        if (currentSession == null)
            return;

        var currentRoundNumber = currentSession.CurrentRound + 1;
        var isBidTotalRuleEnabled = currentSession.BidTotalRuleStartRound > 0;
        var orderedPlayers = currentSession.Players.OrderBy(p => p.Order).ToList();
        var dealerIndexForEntry = currentSession.CurrentRound % orderedPlayers.Count;
        var dealerForEntry = orderedPlayers[dealerIndexForEntry];
        var entryOrder = orderedPlayers
            .Skip(dealerIndexForEntry + 1)
            .Concat(orderedPlayers.Take(dealerIndexForEntry + 1))
            .ToList();

        var trumpOptions = trumpPaletteService.GetTrumpLabels();
        int trumpSelectionIndex = 0;
        var bidEntries = new Dictionary<Guid, Entry>();
        Entry? dealerBidEntry = null;

        var scroll = new ScrollView { BackgroundColor = Colors.White };
        var population = new StackLayout { Spacing = 14, Padding = new Thickness(16, 12), BackgroundColor = Colors.White };
        scroll.Content = population;

        // Header: round number
        population.Children.Add(new Label
        {
            Text = string.Format(Localization.GetString("RoundPopupTitle"), currentRoundNumber, currentSession.MaxRounds),
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        });

        // Trump icon selector (no None option — trump is required)
        population.Children.Add(new Label { Text = Localization.GetString("Trump"), FontAttributes = FontAttributes.Bold });
        Button? doneButton = null;
        Func<int>? getTrumpIndexAccessor = null;
        Label? trumpHintLabelRef = null;
        var (trumpSelectorView, getTrumpIndex) = BuildTrumpIconSelector(() => UpdateTrumpSelectionState());
        population.Children.Add(trumpSelectorView);
       // Friendly hint label
       var trumpHintLabel = new Label
       {
           Text = Localization.GetString("SelectTrumpHint"),
           FontSize = 12,
           TextColor = Colors.Gray,
           HorizontalTextAlignment = TextAlignment.Center,
           Margin = new Thickness(0, -6, 0, 8),
           IsVisible = true
       };
       population.Children.Add(trumpHintLabel);
       getTrumpIndexAccessor = getTrumpIndex;
       trumpHintLabelRef = trumpHintLabel;

        void UpdateTrumpSelectionState()
        {
            var hasTrumpSelection = getTrumpIndexAccessor != null && getTrumpIndexAccessor() >= 0;
            if (trumpHintLabelRef != null)
            {
                trumpHintLabelRef.IsVisible = !hasTrumpSelection;
            }
            if (doneButton != null)
            {
                doneButton.IsEnabled = hasTrumpSelection;
            }
        }


        // Dealer bid total warning (if rule is active and dealer is in round)
        var dealerWarningLabel = new Label
        {
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.DarkRed,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0),
            IsVisible = isBidTotalRuleEnabled && currentRoundNumber >= currentSession.BidTotalRuleStartRound
        };
        
        void UpdateDealerWarning()
        {
            if (isBidTotalRuleEnabled && currentRoundNumber >= currentSession.BidTotalRuleStartRound)
            {
                // Calculate forbidden bid amounts for the dealer
                var otherPlayersBidsSum = bidEntries
                    .Where(kvp => kvp.Key != dealerForEntry.Id)
                    .Sum(kvp => int.TryParse(kvp.Value.Text, out var v) ? v : 0);
                
                var forbiddenBids = new List<int>();
                for (var bid = 0; bid <= currentRoundNumber; bid++)
                {
                    if (otherPlayersBidsSum + bid == currentRoundNumber)
                    {
                        forbiddenBids.Add(bid);
                    }
                }
                
                var forbiddenText = forbiddenBids.Count > 0 
                    ? string.Join(", ", forbiddenBids)
                    : Localization.GetString("None");
                
                dealerWarningLabel.Text = string.Format(
                    Localization.GetString("DealerCannotBidTemplate"),
                    forbiddenText);

                if (dealerBidEntry != null)
                {
                    var dealerBid = int.TryParse(dealerBidEntry.Text, out var parsedBid) ? parsedBid : -1;
                    var isForbidden = forbiddenBids.Contains(dealerBid);
                    dealerBidEntry.BackgroundColor = isForbidden
                        ? Color.FromArgb("#ffe5e5")
                        : Colors.White;
                }
            }
            else if (dealerBidEntry != null)
            {
                dealerBidEntry.BackgroundColor = Colors.White;
            }
        }
        
        if (dealerWarningLabel.IsVisible)
        {
            population.Children.Add(dealerWarningLabel);
            UpdateDealerWarning();
        }

        // Live bid total
        var totalBidsLabel = new Label
        {
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 4)
        };
        void UpdateBidTotal()
        {
            var sum = bidEntries.Values.Sum(entry =>
                int.TryParse(entry.Text, out var v) ? v : 0);
            totalBidsLabel.Text = string.Format(Localization.GetString("TotalBidsLabel"), sum, currentRoundNumber);
            totalBidsLabel.TextColor = sum == currentRoundNumber ? Colors.DarkRed : Colors.DarkGreen;
            UpdateDealerWarning();
        }

        // One entry per player
        population.Children.Add(new Label
        {
            Text = Localization.GetString("BidsPerPlayerLabel"),
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 8, 0, 0)
        });

        foreach (var player in entryOrder)
        {
            var isDealer = player.Id == dealerForEntry.Id;
            var bidEntry = new Entry
            {
                Keyboard = Keyboard.Numeric,
                Text = "0",
                WidthRequest = 52,
                Placeholder = $"0–{currentRoundNumber}",
                HorizontalTextAlignment = TextAlignment.Center
            };
            bidEntry.TextChanged += (s, e) => UpdateBidTotal();
            bidEntries[player.Id] = bidEntry;
            if (isDealer)
            {
                dealerBidEntry = bidEntry;
            }

            var playerLabel = new Label
            {
                Text = isDealer
                    ? $"{player.Name} ({Localization.GetString("DealerLabel")})"
                    : player.Name,
                FontAttributes = isDealer ? FontAttributes.Bold : FontAttributes.None,
                TextColor = isDealer ? Color.FromArgb("#b26a00") : Colors.Black,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Fill
            };

            population.Children.Add(CreateStepperRow(playerLabel, bidEntry, 0, currentRoundNumber));
        }

        population.Children.Add(totalBidsLabel);
        UpdateBidTotal();

        doneButton = new Button
        {
            Text = Localization.GetString("Ok"),
            Margin = new Thickness(0, 12, 0, 0),
            IsEnabled = false
        };
        population.Children.Add(doneButton);
        UpdateTrumpSelectionState();

        var modal = new ContentPage { Content = scroll, BackgroundColor = Colors.White };
        var tcs = new TaskCompletionSource<bool>();

        doneButton.Clicked += async (s, e) =>
        {
            // Trump must be selected.
            if (getTrumpIndex() < 0)
            {
                   trumpHintLabel.IsVisible = true;
                await DisplayAlertAsync(
                       Localization.GetString("InfoTitle"),
                    Localization.GetString("TrumpRequiredError"),
                    Localization.GetString("Ok"));
                return;
            }

               trumpHintLabel.IsVisible = false;
            // Validate all bids.
            foreach (var player in entryOrder)
            {
                var text = bidEntries[player.Id].Text;
                if (!int.TryParse(text, out var bid) || bid < 0 || bid > currentRoundNumber)
                {
                    await DisplayAlertAsync(
                        Localization.GetString("ErrorTitle"),
                        string.Format(Localization.GetString("BidRangeErrorTemplate"), player.Name, currentRoundNumber),
                        Localization.GetString("Ok"));
                    return;
                }
            }

            var totalBids = bidEntries.Values.Sum(entry =>
                int.TryParse(entry.Text, out var bid) ? bid : 0);
            if (isBidTotalRuleEnabled && currentRoundNumber >= currentSession.BidTotalRuleStartRound && totalBids == currentRoundNumber)
            {
                await DisplayAlertAsync(
                    Localization.GetString("ErrorTitle"),
                    string.Format(Localization.GetString("TotalBidsEqualRoundError"), currentRoundNumber),
                    Localization.GetString("Ok"));
                return;
            }

            trumpSelectionIndex = getTrumpIndex();
            tcs.SetResult(true);
        };

        await Navigation.PushModalAsync(modal);
        await tcs.Task;
        await Navigation.PopModalAsync();

        var trump = MapSelectionToTrump(trumpSelectionIndex);
        BackgroundColor = GetTrumpColor(trump);

        var bids = new Dictionary<Guid, int>();
        foreach (var player in orderedPlayers)
        {
            int.TryParse(bidEntries[player.Id].Text, out var bid);
            bids[player.Id] = bid;
        }

        try
        {
            scoreService.StartRound(currentSession, trump, bids);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(Localization.GetString("ErrorTitle"), ex.Message, Localization.GetString("Ok"));
            return;
        }

        // --- Actuals popup ---
        var (trumpSymbol, trumpFgColor, trumpBgColor) = GetTrumpDisplayInfo(trump);
        var lightBg = GetTrumpColor(trump);

        var actualEntries = new Dictionary<Guid, Entry>();
        var scrollActuals = new ScrollView { BackgroundColor = Colors.White };
        var popActuals = new StackLayout { Spacing = 14, Padding = new Thickness(16, 12), BackgroundColor = Colors.White };
        scrollActuals.Content = popActuals;
        var totalActualsLabel = new Label
        {
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 6, 0, 0)
        };

        void UpdateActualsTotal()
        {
            var sum = actualEntries.Values.Sum(entry =>
                int.TryParse(entry.Text, out var value) ? value : 0);
            totalActualsLabel.Text = string.Format(Localization.GetString("TotalActualsLabel"), sum, currentSession.CurrentRound);
            totalActualsLabel.TextColor = sum == currentSession.CurrentRound ? Colors.DarkGreen : Colors.DarkRed;
        }

        // Round title
        popActuals.Children.Add(new Label
        {
            Text = string.Format(Localization.GetString("RoundPopupTitle"), currentSession.CurrentRound, currentSession.MaxRounds),
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        });

        // Trump indicator
        var trumpIndicatorBorder = new Border
        {
            BackgroundColor = trumpBgColor,
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(20, 10),
            HorizontalOptions = LayoutOptions.Center,
            Content = new Label
            {
                Text = trumpSymbol,
                TextColor = trumpFgColor,
                FontSize = 48,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
        popActuals.Children.Add(trumpIndicatorBorder);

        popActuals.Children.Add(new Label
        {
            Text = Localization.GetString("ActualsPerPlayerLabel"),
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 8, 0, 0)
        });

        foreach (var player in entryOrder)
        {
            var bid = bids.GetValueOrDefault(player.Id);
            var isDealer = player.Id == dealerForEntry.Id;
            var actualEntry = new Entry
            {
                Keyboard = Keyboard.Numeric,
                Text = "0",
                WidthRequest = 52,
                Placeholder = $"0\u2013{currentSession.CurrentRound}",
                HorizontalTextAlignment = TextAlignment.Center
            };
            actualEntry.TextChanged += (s, e) => UpdateActualsTotal();
            actualEntries[player.Id] = actualEntry;

            var playerLabel = new Label
            {
                Text = isDealer
                    ? $"{player.Name} ({Localization.GetString("DealerLabel")}, bod: {bid})"
                    : $"{player.Name} (bod: {bid})",
                FontAttributes = isDealer ? FontAttributes.Bold : FontAttributes.None,
                TextColor = isDealer ? Color.FromArgb("#b26a00") : Colors.Black,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Fill
            };

            popActuals.Children.Add(CreateStepperRow(playerLabel, actualEntry, 0, currentSession.CurrentRound));
        }

        popActuals.Children.Add(totalActualsLabel);
        UpdateActualsTotal();

        var doneActuals = new Button
        {
            Text = Localization.GetString("Ok"),
            Margin = new Thickness(0, 12, 0, 0)
        };
        popActuals.Children.Add(doneActuals);

        var tcsActuals = new TaskCompletionSource<bool>();
        doneActuals.Clicked += async (s, e) =>
        {
            var totalActuals = 0;
            foreach (var player in entryOrder)
            {
                var text = actualEntries[player.Id].Text;
                if (!int.TryParse(text, out var act) || act < 0 || act > currentSession.CurrentRound)
                {
                    await DisplayAlertAsync(
                        Localization.GetString("ErrorTitle"),
                        string.Format(Localization.GetString("BidRangeErrorTemplate"), player.Name, currentSession.CurrentRound),
                        Localization.GetString("Ok"));
                    return;
                }
                totalActuals += act;
            }
            // Total tricks won must equal round number.
            if (totalActuals != currentSession.CurrentRound)
            {
                await DisplayAlertAsync(
                    Localization.GetString("ErrorTitle"),
                    string.Format(Localization.GetString("TotalActualsError"), currentSession.CurrentRound),
                    Localization.GetString("Ok"));
                return;
            }
            tcsActuals.SetResult(true);
        };

        await Navigation.PushModalAsync(new ContentPage { Content = scrollActuals, BackgroundColor = Colors.White });
        await tcsActuals.Task;
        await Navigation.PopModalAsync();

        var actuals = new Dictionary<Guid, int>();
        foreach (var player in orderedPlayers)
        {
            int.TryParse(actualEntries[player.Id].Text, out var act);
            actuals[player.Id] = act;
        }

        try
        {
            scoreService.FinishRound(currentSession, actuals);
            if (!currentSession.IsActive)
            {
                SyncSessionStatsToGroup(currentSession);
                highscoreService.UpdateHighscores(groupService.GetGroups());
            }
            RefreshUI();

            if (!currentSession.IsActive && currentSession.CurrentRound >= currentSession.MaxRounds)
            {
                await ShowGameFinishedCelebrationAsync(currentSession);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(Localization.GetString("ErrorTitle"), ex.Message, Localization.GetString("Ok"));
        }
    }

    private void SyncSessionStatsToGroup(ScoreSession session)
    {
        var group = groupService.GetGroup(session.GroupId);
        if (group == null)
            return;

        foreach (var sessionPlayer in session.Players)
        {
            var matchingById = group.Players.FirstOrDefault(p => p.Id == sessionPlayer.Id);
            var matchingByName = group.Players.FirstOrDefault(p =>
                string.Equals(p.Name.Trim(), sessionPlayer.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            var target = matchingById ?? matchingByName;
            if (target == null)
                continue;

            target.Wins = Math.Max(target.Wins, sessionPlayer.Wins);
            target.GamesPlayed = Math.Max(target.GamesPlayed, sessionPlayer.GamesPlayed);
            target.HighestScore = Math.Max(target.HighestScore, sessionPlayer.HighestScore);
            target.CurrentPoints = sessionPlayer.CurrentPoints;
        }

        groupService.UpdateGroup(group);
    }

    private (View view, Func<int> getSelectedIndex) BuildTrumpIconSelector(Action? onSelectionChanged = null)
    {
        var mode = trumpPaletteService.GetMode();

        // (symbol/label, foreground color, background tint, suit index 0=None…4=Spades)
        // None option removed — trump is required.
        var options = mode == TrumpPaletteMode.FourColors
            ? new[]
            {
                ("●",  Colors.White,  Colors.Red),
                ("●",  Colors.White,  Colors.Yellow),
                ("●",  Colors.White,  Colors.Green),
                ("●",  Colors.White,  Colors.Blue),
            }
            : new[]
            {
                ("♥",  Colors.Red,                    Colors.White),
                ("♦",  Color.FromArgb("#e05000"),      Colors.White),
                ("♣",  Colors.DarkGreen,               Colors.White),
                ("♠",  Colors.DarkBlue,                Colors.White),
            };

        int selectedIndex = -1;  // nothing pre-selected; user must pick
        var buttons = new List<Border>();
        var labels = new List<Label>();

        Color SelectedStroke  = Colors.Black;
        Color UnselectedStroke = Colors.LightGray;

        void UpdateHighlight()
        {
            for (var i = 0; i < buttons.Count; i++)
            {
                buttons[i].Stroke = i == selectedIndex ? SelectedStroke : UnselectedStroke;
                if (mode == TrumpPaletteMode.FourColors)
                {
                    labels[i].TextColor = i == selectedIndex ? Colors.Black : Colors.White;
                }
            }
            onSelectionChanged?.Invoke();
        }

        var row = new HorizontalStackLayout { Spacing = 8 };

        for (var i = 0; i < options.Length; i++)
        {
            var (symbol, fg, bg) = options[i];
            var idx = i;

            var label = new Label
            {
                Text = symbol,
                TextColor = fg,
                FontSize = 28,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                WidthRequest = 48,
                HeightRequest = 48
            };
            labels.Add(label);

            var border = new Border
            {
                WidthRequest = 52,
                HeightRequest = 52,
                BackgroundColor = bg,
                Stroke = UnselectedStroke,
                StrokeThickness = 2,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Content = label,
                Padding = new Thickness(0)
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) =>
            {
                selectedIndex = idx;
                UpdateHighlight();
            };
            border.GestureRecognizers.Add(tap);

            buttons.Add(border);
            row.Children.Add(border);
        }

        return (row, () => selectedIndex);
    }

    private static View CreateStepperRow(Label nameLabel, Entry entry, int min, int max)
    {
        var minusBtn = new Button
        {
            Text = "−",
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 8,
            Padding = new Thickness(0),
            FontSize = 18,
            BackgroundColor = Color.FromArgb("#e0e8f0"),
            TextColor = Color.FromArgb("#1a3a5c")
        };
        var plusBtn = new Button
        {
            Text = "+",
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 8,
            Padding = new Thickness(0),
            FontSize = 18,
            BackgroundColor = Color.FromArgb("#1a3a5c"),
            TextColor = Colors.White
        };

        void Refresh()
        {
            int.TryParse(entry.Text, out var v);
            minusBtn.IsEnabled = v > min;
            plusBtn.IsEnabled = v < max;
        }

        minusBtn.Clicked += (s, e) =>
        {
            if (int.TryParse(entry.Text, out var v) && v > min)
                entry.Text = (v - 1).ToString();
        };
        plusBtn.Clicked += (s, e) =>
        {
            if (int.TryParse(entry.Text, out var v) && v < max)
                entry.Text = (v + 1).ToString();
        };
        entry.TextChanged += (s, e) => Refresh();
        Refresh();

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 6
        };
        grid.Add(nameLabel, 0, 0);
        grid.Add(minusBtn, 1, 0);
        grid.Add(entry, 2, 0);
        grid.Add(plusBtn, 3, 0);
        return grid;
    }

    private (string symbol, Color fg, Color bg) GetTrumpDisplayInfo(TrumpSuit trump)
    {
        if (trumpPaletteService.GetMode() == TrumpPaletteMode.FourColors)
        {
            return trump switch
            {
                TrumpSuit.Hearts   => ("●", Colors.White, Colors.Red),
                TrumpSuit.Diamonds => ("●", Colors.White, Colors.Yellow),
                TrumpSuit.Clubs    => ("●", Colors.White, Colors.Green),
                TrumpSuit.Spades   => ("●", Colors.White, Colors.Blue),
                _                  => ("?", Colors.Gray, Colors.White)
            };
        }
        return trump switch
        {
            TrumpSuit.Hearts   => ("♥", Colors.Red,                   Colors.White),
            TrumpSuit.Diamonds => ("♦", Color.FromArgb("#e05000"),    Colors.White),
            TrumpSuit.Clubs    => ("♣", Colors.DarkGreen,              Colors.White),
            TrumpSuit.Spades   => ("♠", Colors.DarkBlue,              Colors.White),
            _                  => ("?", Colors.Gray,                   Colors.White)
        };
    }

    private Color GetTrumpColor(TrumpSuit trump)
    {
        if (trumpPaletteService.GetMode() == TrumpPaletteMode.FourColors)
        {
            return trump switch
            {
                TrumpSuit.Hearts => Colors.Red.WithAlpha(0.2f),
                TrumpSuit.Diamonds => Colors.Yellow.WithAlpha(0.25f),
                TrumpSuit.Clubs => Colors.Green.WithAlpha(0.2f),
                TrumpSuit.Spades => Colors.Blue.WithAlpha(0.2f),
                _ => Colors.White
            };
        }

        return trump switch
        {
            TrumpSuit.Hearts => Colors.Red.WithAlpha(0.2f),
            TrumpSuit.Diamonds => Colors.Orange.WithAlpha(0.2f),
            TrumpSuit.Clubs => Colors.Green.WithAlpha(0.2f),
            TrumpSuit.Spades => Colors.Blue.WithAlpha(0.2f),
            _ => Colors.White
        };
    }

    private static TrumpSuit MapSelectionToTrump(int selectedIndex)
    {
        // None removed from selector; 0=Hearts, 1=Diamonds, 2=Clubs, 3=Spades.
        return selectedIndex switch
        {
            0 => TrumpSuit.Hearts,
            1 => TrumpSuit.Diamonds,
            2 => TrumpSuit.Clubs,
            3 => TrumpSuit.Spades,
            _ => TrumpSuit.None
        };
    }

    private async Task ShowGameFinishedCelebrationAsync(ScoreSession session)
    {
        if (session.Players.Count == 0)
            return;

        var bestScore = session.Players.Max(p => p.CurrentPoints);
        var winners = session.Players
            .Where(p => p.CurrentPoints == bestScore)
            .OrderBy(p => p.Order)
            .ToList();
        var winnerNames = string.Join(", ", winners.Select(w => w.Name));

        var tcs = new TaskCompletionSource<bool>();
        var random = new Random();
        var confettiSymbols = new[] { "🎉", "🎊", "✨", "🎈", "🥳" };

        var confettiLayer = new AbsoluteLayout
        {
            InputTransparent = true
        };

        var confettiLabels = new List<Label>();
        for (var i = 0; i < 26; i++)
        {
            var lbl = new Label
            {
                Text = confettiSymbols[random.Next(confettiSymbols.Length)],
                FontSize = random.Next(16, 25),
                Opacity = 0.9
            };
            AbsoluteLayout.SetLayoutFlags(lbl, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);
            AbsoluteLayout.SetLayoutBounds(lbl, new Rect(random.NextDouble(), -0.2 - random.NextDouble(), AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
            confettiLayer.Children.Add(lbl);
            confettiLabels.Add(lbl);
        }

        var closeButton = new Button
        {
            Text = Localization.GetString("Ok"),
            Margin = new Thickness(0, 12, 0, 0)
        };

        var card = new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Color.FromArgb("#d6dce5"),
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(20, 16),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label
                    {
                        Text = "🎉🎊🎉",
                        FontSize = 30,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    new Label
                    {
                        Text = Localization.GetString("GameFinishedTitle"),
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 22,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    new Label
                    {
                        Text = string.Format(Localization.GetString("WinnerTemplate"), winnerNames, bestScore),
                        FontSize = 18,
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    closeButton
                }
            }
        };

        var root = new Grid
        {
            BackgroundColor = Color.FromArgb("#22000000")
        };
        root.Children.Add(confettiLayer);
        root.Children.Add(card);

        var modal = new ContentPage
        {
            Content = root,
            BackgroundColor = Colors.Transparent
        };

        closeButton.Clicked += (s, e) => tcs.TrySetResult(true);
        modal.Disappearing += (s, e) => tcs.TrySetResult(true);

        await Navigation.PushModalAsync(modal);

        _ = Task.Run(async () =>
        {
            foreach (var label in confettiLabels)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var travel = 700 + random.Next(0, 260);
                    await label.TranslateToAsync(0, travel, (uint)random.Next(1400, 2300), Easing.CubicIn);
                });
            }
        });

        await tcs.Task;
        if (Navigation.ModalStack.Contains(modal))
            await Navigation.PopModalAsync();

        await Shell.Current.GoToAsync(nameof(HighscorePage));
    }
}

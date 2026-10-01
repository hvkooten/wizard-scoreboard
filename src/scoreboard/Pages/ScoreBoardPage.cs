using Microsoft.Maui.Layouts;
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
    private readonly IScreenWakeService screenWakeService;
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
    private Button editLastRoundButton;
    private readonly ContentView buttonBar = new();
    private bool simplifiedButtons;
    private ScrollView scoreboardScrollView;
    private readonly Dictionary<(Grid Grid, int Row, int Column), Border> scoreboardCells = new();

    // Palette for header colours
    private static Color HeaderBg => AppColors.Primary;
    private static Color HeaderFg => Colors.White;
    private static Color RowEven => AppColors.SurfaceAlt;
    private static Color RowOdd => AppColors.Surface;
    private static Color WinBg => Color.FromArgb(AppColors.IsDark ? "#1f4a2a" : "#c8f7c5");
    private static Color LoseBg => AppColors.DangerBg;
    private static Color WinFg => Color.FromArgb(AppColors.IsDark ? "#8fe39f" : "#1a6b2a");
    private static Color LoseFg => AppColors.DangerText;
    private static Color TotalBg => Color.FromArgb(AppColors.IsDark ? "#1e3550" : "#ddeeff");

    public ScoreBoardPage(IGroupService groupService, IHighscoreService highscoreService, IScoreService scoreService, ITrumpPaletteService trumpPaletteService, IScreenWakeService screenWakeService)
    {
        PageTitleHelper.Apply(this, Localization.GetString("Scoreboard"));

        this.groupService = groupService;
        this.highscoreService = highscoreService;
        this.scoreService = scoreService;
        this.trumpPaletteService = trumpPaletteService;
        this.screenWakeService = screenWakeService;

        statusLabel = new Label
        {
            FontAttributes = FontAttributes.Bold,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };

        scoreGrid = new Grid();
        headerGrid = new Grid();
        footerGrid = new Grid();

        simplifiedButtons = AppSettings.SimplifiedButtons;
        (buttonBar.Content, startButton, nextRoundButton, editLastRoundButton, pauseButton, endButton) =
            CreateActionBar(simplifiedButtons);

        scoreboardScrollView = new ScrollView
        {
            BackgroundColor = AppColors.Surface,
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
            BackgroundColor = AppColors.Surface,
            Children = { statusLabel, buttonBar }
        };

        mainGrid.Add(headerStack, 0, 0);
        mainGrid.Add(headerGrid, 0, 1);
        mainGrid.Add(scoreboardScrollView, 0, 2);
        mainGrid.Add(footerGrid, 0, 3);

        Content = mainGrid;

        // Rebuild the dynamically-built content when the global bold-text setting changes so the
        // currently visible page updates immediately instead of only after the next rebuild.
        App.GlobalTextStyleChanged += RefreshUI;

        RefreshUI();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Shell reuses this page, so another saved game may have been selected while it was hidden.
        var selectedSession = scoreService.GetCurrentSession();
        if (selectedSession != null || currentSession?.IsActive == true)
        {
            currentSession = selectedSession;
        }

        if (currentSession != null)
        {
            groupService.SetSelectedGroup(currentSession.GroupId);
        }
        else
        {
            var selectedGroup = groupService.GetSelectedGroup();
            if (selectedGroup != null)
            {
                groupService.SetSelectedGroup(selectedGroup.Id);
            }
        }

        RefreshUI();
    }

    private (View Layout, Button Start, Button NextRound, Button Edit, Button Pause, Button End) CreateActionBar(bool simplified)
    {
        var start = CreateActionButton("StartGame", StartGameAsync);
        var nextRound = CreateActionButton("NextRound", StartNextRoundAsync);
        var edit = CreateActionButton("EditLastRound", EditLastRoundAsync, simplified ? "\u270E" : null);
        var pause = CreateActionButton("PauseGame", TogglePauseAsync, simplified ? "\u23F8" : null);
        var end = CreateActionButton("EndGame", EndGameAsync, simplified ? "\u23F9" : null);
        var primaryButtons = new FlexLayout
        {
            Direction = FlexDirection.Row,
            Wrap = FlexWrap.Wrap,
            Children = { start, nextRound }
        };

        if (!simplified)
        {
            var secondaryTextButtons = new FlexLayout
            {
                Direction = FlexDirection.Row,
                Wrap = FlexWrap.Wrap,
                JustifyContent = FlexJustify.End,
                Children = { edit, pause, end }
            };
            end.Margin = new Thickness(0, 0, 0, 8);
            var textLayout = new VerticalStackLayout
            {
                Children = { primaryButtons, secondaryTextButtons }
            };
            return (textLayout, start, nextRound, edit, pause, end);
        }

        var secondaryButtons = new HorizontalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 0, 0, 8),
            Children = { edit, pause, end }
        };
        var layout = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        layout.Add(primaryButtons);
        layout.Add(secondaryButtons, 1);
        return (layout, start, nextRound, edit, pause, end);
    }

    private static Button CreateActionButton(string resourceKey, Func<Task> actionAsync, string? icon = null)
    {
        var button = new Button
        {
            Margin = new Thickness(0, 0, 8, 8),
            FontAttributes = AppSettings.BoldAllText ? FontAttributes.Bold : FontAttributes.None
        };
        if (icon != null)
        {
            button.WidthRequest = 48;
            button.HeightRequest = 48;
            button.MinimumWidthRequest = 48;
            button.MinimumHeightRequest = 48;
            button.Padding = new Thickness(0);
            button.Margin = new Thickness(0);
            button.FontSize = 22;
            button.VerticalOptions = LayoutOptions.Start;
        }
        SetActionCaption(button, resourceKey, icon ?? string.Empty, icon != null);
        button.Clicked += async (s, e) => await actionAsync();
        return button;
    }

    private void UpdateActionButtons(bool isPaused)
    {
        var simplified = AppSettings.SimplifiedButtons;
        if (simplifiedButtons != simplified)
        {
            // Fresh controls avoid retaining native measurements when switching between text and icons.
            (buttonBar.Content, startButton, nextRoundButton, editLastRoundButton, pauseButton, endButton) =
                CreateActionBar(simplified);
            simplifiedButtons = simplified;
        }

        SetActionCaption(editLastRoundButton, "EditLastRound", "\u270E", simplified);
        SetActionCaption(pauseButton, isPaused ? "ResumeGame" : "PauseGame", isPaused ? "\u25B6" : "\u23F8", simplified);
        SetActionCaption(endButton, "EndGame", "\u23F9", simplified);
    }

    private static void SetActionCaption(Button button, string resourceKey, string icon, bool simplified)
    {
        var caption = Localization.GetString(resourceKey);
        button.Text = simplified ? icon : caption;
        SemanticProperties.SetDescription(button, caption);
        ToolTipProperties.SetText(button, caption);
    }

    private void RefreshUI()
    {
        var rebuildGrid = currentSession == null
            || headerGrid.RowDefinitions.Count != 1
            || scoreGrid.ColumnDefinitions.Count != currentSession.Players.Count + 2
            || scoreGrid.RowDefinitions.Count != currentSession.MaxRounds * 2;
        if (rebuildGrid)
        {
            scoreboardCells.Clear();
            headerGrid.Children.Clear();
            headerGrid.RowDefinitions.Clear();
            headerGrid.ColumnDefinitions.Clear();
            scoreGrid.Children.Clear();
            scoreGrid.RowDefinitions.Clear();
            scoreGrid.ColumnDefinitions.Clear();
            footerGrid.Children.Clear();
            footerGrid.RowDefinitions.Clear();
            footerGrid.ColumnDefinitions.Clear();
        }

        var hasAvailableGroup = groupService.GetSelectedGroup() != null || groupService.GetGroups().Any();
        var hasSession = currentSession != null;
        var isActiveSession = currentSession?.IsActive == true;
        var isPausedSession = currentSession?.IsPaused == true;
        var hasRoundsRemaining = isActiveSession
            && !isPausedSession
            && currentSession!.CurrentRound < currentSession.MaxRounds;

        UpdateActionButtons(isPausedSession);
        startButton.IsEnabled = hasAvailableGroup && (!hasSession || isPausedSession);
        pauseButton.IsEnabled = isActiveSession;
        nextRoundButton.IsEnabled = hasRoundsRemaining;
        editLastRoundButton.IsEnabled = isActiveSession && !isPausedSession
            && currentSession?.Rounds.LastOrDefault()?.ActualByPlayer.Count > 0;
        endButton.IsEnabled = hasSession;

        // Keep the screen on only while a game is actually in progress.
        if (isActiveSession && !isPausedSession)
        {
            screenWakeService.RequestKeepAwake();
        }
        else
        {
            screenWakeService.ReleaseKeepAwake();
        }

        // Show only actions that are currently available.
        startButton.IsVisible = startButton.IsEnabled;
        pauseButton.IsVisible = pauseButton.IsEnabled;
        nextRoundButton.IsVisible = nextRoundButton.IsEnabled;
        editLastRoundButton.IsVisible = editLastRoundButton.IsEnabled;
        endButton.IsVisible = endButton.IsEnabled;

        if (currentSession == null)
        {
            statusLabel.Text = Localization.GetString("NoActiveGame");
            BackgroundColor = AppColors.Surface;

            // Show players
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
            else if (!hasAvailableGroup)
            {
                // First run / no groups yet: guide the user to create one.
                scoreGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                scoreGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                scoreGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var infoLbl = new Label
                {
                    Text = Localization.GetString("NoGroupsAvailable"),
                    FontSize = 15,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Padding = new Thickness(12, 12)
                };
                scoreGrid.Add(infoLbl, 0, 0);

                var createGroupButton = new Button
                {
                    Text = Localization.GetString("CreateGroup"),
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                createGroupButton.Clicked += async (_, _) =>
                    await Shell.Current.GoToAsync($"//{nameof(GroupsPage)}");
                scoreGrid.Add(createGroupButton, 0, 1);
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
        if (rebuildGrid)
        {
            foreach (var grid in new[] { headerGrid, scoreGrid, footerGrid })
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = 36 });  // Rnd
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = 36 });  // Dealer / trump
                foreach (var _ in players)
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            }
            headerGrid.RowDefinitions.Add(new RowDefinition { Height = 36 });
            footerGrid.RowDefinitions.Add(new RowDefinition { Height = 32 });
        }

        // ── Header row ────────────────────────────────────────────────────
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
                ? Color.FromArgb(AppColors.IsDark ? "#3a3320" : "#fff8e1")
                : (isEven ? RowEven : RowOdd);
            var gridRow = (roundNum - 1) * 2;
            if (rebuildGrid)
            {
                scoreGrid.RowDefinitions.Add(new RowDefinition { Height = 22 });  // bid row
                scoreGrid.RowDefinitions.Add(new RowDefinition { Height = 28 });  // total row
            }

            // Round number (spans 2 rows)
            var rndLabel = UpdateCell(scoreGrid, roundNum.ToString(), gridRow, 0,
                bold: isCurrentRound, fontSize: 13, center: true, bg: rowBg);
            Grid.SetRowSpan(rndLabel, 2);

            // Trump symbol (spans 2 rows) — shows dot for future rounds
            string trumpText = round != null ? TrumpSymbol(round.Trump) : FutureRoundTrumpSymbol();
            Color trumpFg = round != null ? TrumpColor(round.Trump) : Colors.LightGray;
            var trumpLbl = UpdateCell(scoreGrid, trumpText, gridRow, 1,
                bold: true, fontSize: 16, center: true, bg: rowBg, fg: trumpFg);
            Grid.SetRowSpan(trumpLbl, 2);

            for (var c = 0; c < players.Count; c++)
            {
                var pid = players[c].Id;

                if (round == null)
                {
                    // Future round — empty cells
                    UpdateCell(scoreGrid, "", gridRow, 2 + c, bg: rowBg);
                    UpdateCell(scoreGrid, "", gridRow + 1, 2 + c, bg: rowBg);
                    continue;
                }

                var bid = round.BidByPlayer.GetValueOrDefault(pid, -1);
                var actual = round.ActualByPlayer.GetValueOrDefault(pid, -1);

                Color cellBg;
                Color bidFg = Colors.Gray;

                if (actual >= 0)
                {
                    var delta = bid == actual ? 2 + actual : -(Math.Abs(bid - actual));
                    runningTotals[pid] += delta;
                    cellBg = delta >= 0 ? WinBg : LoseBg;
                    bidFg = delta >= 0 ? WinFg : LoseFg;
                }
                else
                {
                    cellBg = rowBg;
                }

                // Top row: show bid and actual wins together.
                var bidText = bid >= 0
                    ? (actual >= 0 ? $"{bid}/{actual}" : $"{bid}/?")
                    : "-";
                UpdateCell(scoreGrid, bidText, gridRow, 2 + c, fontSize: 11, fg: bidFg, bg: cellBg,
                    padding: new Thickness(3, 1, 0, 0));

                // Total row: large, centred
                var totalText = actual >= 0 ? runningTotals[pid].ToString() : "";
                var totalFg = runningTotals[pid] >= 0 ? WinFg : LoseFg;
                UpdateCell(scoreGrid, totalText, gridRow + 1, 2 + c, bold: true, fontSize: 15,
                    fg: actual >= 0 ? totalFg : AppColors.TextPrimary);
            }
        }

        // ── Total row (always visible) ────────────────────────────────────
        var totHdr = UpdateCell(footerGrid, Localization.GetString("TotalHeader"), 0, 0,
            bold: true, fontSize: 13, center: true, bg: TotalBg);
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

    private Border UpdateCell(Grid grid, string text, int row, int col,
        bool bold = false, double fontSize = 13,
        bool center = false, Color? bg = null, Color? fg = null,
        Thickness? padding = null, Color? stroke = null, double strokeThickness = 0.75)
    {
        var key = (grid, row, col);
        var isNew = !scoreboardCells.TryGetValue(key, out var border);
        if (isNew)
        {
            border = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 0 },
                Padding = 0,
                Content = new Label { VerticalTextAlignment = TextAlignment.Center }
            };
        }

        var lbl = (Label)border!.Content!;
        if (lbl.Text != text) lbl.Text = text;
        var attributes = bold ? FontAttributes.Bold : FontAttributes.None;
        if (lbl.FontAttributes != attributes) lbl.FontAttributes = attributes;
        if (lbl.FontSize != fontSize) lbl.FontSize = fontSize;
        var alignment = center ? TextAlignment.Center : TextAlignment.Start;
        if (lbl.HorizontalTextAlignment != alignment) lbl.HorizontalTextAlignment = alignment;
        var cellPadding = padding ?? new Thickness(2);
        if (lbl.Padding != cellPadding) lbl.Padding = cellPadding;
        if (fg != null)
        {
            if (lbl.TextColor != fg) lbl.TextColor = fg;
        }
        else if (lbl.IsSet(Label.TextColorProperty))
        {
            lbl.ClearValue(Label.TextColorProperty);
        }

        var background = bg ?? Colors.Transparent;
        if (border.BackgroundColor != background) border.BackgroundColor = background;
        var strokeColor = stroke ?? AppColors.Border;
        if (border.Stroke is not SolidColorBrush brush || brush.Color != strokeColor)
            border.Stroke = new SolidColorBrush(strokeColor);
        if (border.StrokeThickness != strokeThickness) border.StrokeThickness = strokeThickness;

        if (isNew)
        {
            scoreboardCells.Add(key, border);
            grid.Add(border, col, row);
        }
        return border;
    }

    private void AddHeaderCell(Grid grid, string text, int row, int col, bool isDealer = false)
    {
        UpdateCell(grid, text, row, col, bold: true, fontSize: isDealer ? 14 : 13,
            center: true, fg: isDealer ? Colors.Black : HeaderFg,
            bg: isDealer ? Color.FromArgb("#ffd54f") : HeaderBg,
            padding: new Thickness(4, 2), stroke: Color.FromArgb("#8aa0b8"), strokeThickness: 1);
    }

    private void AddCell(Grid grid, string text, int row, int col,
        bool bold = false, bool center = false, Color? color = null, Color? bg = null, bool small = false)
    {
        UpdateCell(grid, text, row, col, bold: bold, fontSize: small ? 11 : 13,
            center: center, fg: color, bg: bg, padding: new Thickness(4, 2));
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
            TrumpSuit.Hearts => "♥",
            TrumpSuit.Diamonds => "♦",
            TrumpSuit.Clubs => "♣",
            TrumpSuit.Spades => "♠",
            _ => "—"
        };
    }

    private Color TrumpColor(TrumpSuit trump) => trump switch
    {
        TrumpSuit.Hearts => Colors.Red,
        TrumpSuit.Diamonds => trumpPaletteService.GetMode() == TrumpPaletteMode.FourColors
            ? Colors.Goldenrod
            : Color.FromArgb("#e05000"),
        TrumpSuit.Clubs => Colors.DarkGreen,
        TrumpSuit.Spades => Colors.DarkBlue,
        _ => Colors.Gray
    };

    private async Task StartGameAsync()
    {
        await DisableGameButtonsAsync();

        try
        {
            await StartGameCoreAsync();
        }
        finally
        {
            RefreshUI();
        }
    }

    private async Task StartGameCoreAsync()
    {
        var group = groupService.GetSelectedGroup() ?? groupService.GetGroups().FirstOrDefault();
        if (group == null)
        {
            await this.ShowMessageAsync("ErrorTitle", Localization.GetString("NoGroupsAvailable"));
            return;
        }

        groupService.SetSelectedGroup(group.Id);

        // The player-selection modal lets the user switch groups or jump to group creation,
        // so keep re-showing it until the user starts a game or cancels.
        while (true)
        {
            var result = await SelectPlayersAsync(group);

            if (result.CreateNewGroup || result.EditGroup)
            {
                GroupsPage.SelectNewGroupOnAppearing = result.CreateNewGroup;
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
            return;
        }
    }

    private sealed record PlayerSelectionResult(List<Player>? Players, bool SwitchGroup = false, bool EditGroup = false, bool CreateNewGroup = false);

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
        var followsPlayerCount = group.BidTotalRuleStartRound is < 0 or > Group.MaxRoundLimit;
        var followsDoublePlayerCount = group.BidTotalRuleStartRound == Group.DoublePlayerCountRule;
        int FollowValue() => Group.PlayerCountStartRound(selected.Count, followsDoublePlayerCount);
        int MaxRound() => Group.MaxRounds(selected.Count);
        var bidRuleValue = Math.Min(followsPlayerCount ? FollowValue() : group.BidTotalRuleStartRound, MaxRound());
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

        void ApplyStyle(Button btn, bool isOn) => UiFactory.ApplyToggleStyle(btn, isOn);

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
            tcs.TrySetResult(new PlayerSelectionResult(null, SwitchGroup: true));
        };

        var newGroupBtn = new Button
        {
            Text = Localization.GetString("NewGroup"),
            HeightRequest = 44,
            HorizontalOptions = LayoutOptions.Fill
        };
        newGroupBtn.Clicked += (s, e) =>
            tcs.TrySetResult(new PlayerSelectionResult(null, CreateNewGroup: true));

        // Opens the Groups page with the current group selected so it can be edited.
        var editGroupBtn = new Button
        {
            Text = Localization.GetString("EditGroup"),
            HeightRequest = 44,
            HorizontalOptions = LayoutOptions.Fill
        };
        editGroupBtn.Clicked += (s, e) =>
            tcs.TrySetResult(new PlayerSelectionResult(null, EditGroup: true));

        groupPicker.HorizontalOptions = LayoutOptions.Fill;
        layout.Children.Add(groupPicker);

        // The play order (used for dealer rotation) follows this list, which the user can
        // rearrange by dragging one player onto another.
        var orderedPlayers = new List<Player>(allPlayers);

        var playersContainer = new VerticalStackLayout { Spacing = 8 };

        void MovePlayer(Guid playerId, int direction)
        {
            var from = orderedPlayers.FindIndex(p => p.Id == playerId);
            if (from < 0)
                return;

            var to = from + direction;
            if (to < 0 || to >= orderedPlayers.Count)
                return;

            (orderedPlayers[from], orderedPlayers[to]) = (orderedPlayers[to], orderedPlayers[from]);
            BuildPlayerButtons();
        }

        void BuildPlayerButtons()
        {
            playersContainer.Children.Clear();
            toggleButtons.Clear();

            for (var index = 0; index < orderedPlayers.Count; index++)
            {
                var capturedId = orderedPlayers[index].Id;
                var btn = new Button
                {
                    Text = orderedPlayers[index].Name,
                    HeightRequest = 44,
                    CornerRadius = 8,
                    FontSize = 16,
                    HorizontalOptions = LayoutOptions.Fill
                };
                ApplyStyle(btn, selected.Contains(capturedId));
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

                var upBtn = UiFactory.CreateReorderButton("\u25B2", index > 0, (s, e) => MovePlayer(capturedId, -1));
                var downBtn = UiFactory.CreateReorderButton("\u25BC", index < orderedPlayers.Count - 1, (s, e) => MovePlayer(capturedId, 1));

                var row = new Grid
                {
                    ColumnSpacing = 6,
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition { Width = GridLength.Auto }
                    }
                };
                row.Add(btn, 0);
                row.Add(upBtn, 1);
                row.Add(downBtn, 2);

                toggleButtons[capturedId] = btn;
                playersContainer.Children.Add(row);
            }
        }

        BuildPlayerButtons();

        layout.Children.Add(new Label
        {
            Text = Localization.GetString("DragToReorderHint"),
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4)
        });
        layout.Children.Add(playersContainer);

        layout.Children.Add(countLabel);

        // Bid total rule start round setting
        var bidRuleLabel = new Label
        {
            FontSize = 13,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 12, 0, 4)
        };

        var bidRuleMinusBtn = UiFactory.CreateStepperButton("−", isPrimary: false);
        var bidRulePlusBtn = UiFactory.CreateStepperButton("+", isPrimary: true);

        void UpdateBidRuleUI()
        {
            var bidRuleDisplay = bidRuleValue == 0
                ? Localization.GetString("Disabled")
                : bidRuleValue.ToString();
            bidRuleLabel.Text = $"{Localization.GetString("BidTotalRuleStartRound")}: {bidRuleDisplay}";
            bidRuleMinusBtn.IsEnabled = bidRuleValue > 0;
            bidRulePlusBtn.IsEnabled = bidRuleValue < MaxRound();
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
            if (bidRuleValue < MaxRound())
            {
                bidRuleValue++;
                UpdateBidRuleUI();
            }
        };

        syncBidRuleWithPlayers = () =>
        {
            bidRuleValue = followsPlayerCount
                ? Math.Min(FollowValue(), MaxRound())
                : Math.Min(bidRuleValue, MaxRound());
            UpdateBidRuleUI();
        };

        UpdateBidRuleUI();

        var bidRuleRow = new HorizontalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center,
            Children = { bidRuleMinusBtn, bidRuleLabel, bidRulePlusBtn }
        };
        layout.Children.Add(bidRuleRow);

        var noTrumpSwitch = new Switch { IsToggled = group.AllowNoTrump, VerticalOptions = LayoutOptions.Center };
        layout.Children.Add(new HorizontalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = Localization.GetString("AllowNoTrump"), FontSize = 13, VerticalOptions = LayoutOptions.Center },
                noTrumpSwitch
            }
        });

        var cancelBtn = new Button { Text = Localization.GetString("Cancel"), HorizontalOptions = LayoutOptions.Fill };
        startBtn.HorizontalOptions = LayoutOptions.Fill;
        var buttonRow = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        buttonRow.Add(startBtn, 0);
        buttonRow.Add(cancelBtn, 1);
        layout.Children.Add(buttonRow);
        var groupButtonRow = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        groupButtonRow.Add(editGroupBtn, 0);
        groupButtonRow.Add(newGroupBtn, 1);
        layout.Children.Add(groupButtonRow);
        UpdateUI();

        var modal = new ContentPage { Content = new ScrollView { Content = layout } };

        startBtn.Clicked += (s, e) =>
        {
            Preferences.Default.Set(prefKey, string.Join(',', selected.Select(id => id.ToString())));
            // Persist the drag-and-drop play order onto the group's players.
            for (var i = 0; i < orderedPlayers.Count; i++)
                orderedPlayers[i].Order = i;
            // Preserve the follow-player-count mode in storage; otherwise persist the fixed round.
            group.BidTotalRuleStartRound = followsPlayerCount
                ? (followsDoublePlayerCount ? Group.DoublePlayerCountRule : Group.PlayerCountRule)
                : bidRuleValue;
            group.AllowNoTrump = noTrumpSwitch.IsToggled;
            groupService.UpdateGroup(group);
            // Apply the effective value (including any manual override) to the in-memory group so the
            // game about to start uses it, without overwriting the persisted follow-player-count mode.
            group.BidTotalRuleStartRound = bidRuleValue;
            var result = orderedPlayers.Where(p => selected.Contains(p.Id)).ToList();
            tcs.TrySetResult(new PlayerSelectionResult(result));
        };

        cancelBtn.Clicked += (s, e) =>
        {
            tcs.TrySetResult(new PlayerSelectionResult(null));
        };

        modal.Disappearing += (s, e) => tcs.TrySetResult(new PlayerSelectionResult(null));

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
            await this.ShowMessageAsync("InfoTitle", Localization.GetString("NoActiveGame"));
            return;
        }

        if (currentSession.IsPaused)
        {
            await this.ShowMessageAsync("InfoTitle", Localization.GetString("GamePausedStatus"));
            return;
        }

        if (currentSession.CurrentRound >= currentSession.MaxRounds)
        {
            await this.ShowMessageAsync("InfoTitle", Localization.GetString("GameAlreadyCompleted"));
            return;
        }

        var session = currentSession;
        await DisableGameButtonsAsync();

        try
        {
            await DisplayRoundPopupAsync();
        }
        finally
        {
            if (session.Rounds.LastOrDefault() is { ActualByPlayer.Count: 0 })
            {
                scoreService.CancelRound(session);
            }
            RefreshUI();
        }
    }

    // Block further clicks
    // one frame to show the disabled state before the (slower) work starts.
    private async Task DisableGameButtonsAsync()
    {
        startButton.IsEnabled = false;
        pauseButton.IsEnabled = false;
        endButton.IsEnabled = false;
        nextRoundButton.IsEnabled = false;
        editLastRoundButton.IsEnabled = false;
        await Task.Delay(16);
    }

    private async Task TogglePauseAsync()
    {
        if (currentSession == null || !currentSession.IsActive)
            return;

        await DisableGameButtonsAsync();

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
            RefreshUI();
            await this.ShowMessageAsync("ErrorTitle", ex.Message);
        }
    }

    private async Task EditLastRoundAsync()
    {
        var session = currentSession;
        var round = session?.Rounds.LastOrDefault();
        if (session is not { IsActive: true, IsPaused: false }
            || round is not { ActualByPlayer.Count: > 0 })
        {
            return;
        }

        await DisableGameButtonsAsync();
        try
        {
            var actuals = await DisplayActualsPopupAsync(session, round);
            if (actuals != null)
            {
                scoreService.UpdateLastRoundActuals(session, actuals);
            }
        }
        finally
        {
            RefreshUI();
        }
    }

    private async Task DisplayRoundPopupAsync(Dictionary<Guid, int>? initialBids = null, int initialTrumpIndex = -1, ContentPage? existingModal = null)
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

        // Running totals so far, shown behind each player's name during bidding.
        var scoreTotals = SessionScoreCalculator.CalculateSessionScores(currentSession);

        var trumpOptions = trumpPaletteService.GetTrumpLabels();
        int trumpSelectionIndex = 0;
        var bidEntries = new Dictionary<Guid, Entry>();
        var touchedBids = new HashSet<Guid>();
        Entry? dealerBidEntry = null;
        Action? refreshDealerColor = null;

        var scroll = new ScrollView { BackgroundColor = Colors.Transparent };
        var population = new StackLayout { Spacing = 14, Padding = new Thickness(16, 12), BackgroundColor = Colors.Transparent };
        scroll.Content = population;

        // Large watermark of the current round number shown behind the bidding form.
        // Its color follows a dark version of the chosen trump, or gray when none is selected yet.
        var roundWatermark = CreateRoundWatermark(currentRoundNumber, TrumpSuit.None);
        var noTrumpBg = AppColors.IsDark ? Colors.Black : AppColors.Surface;
        var bidFooter = CreateFormFooter();
        var bidBackdrop = CreateWatermarkBackdrop(noTrumpBg, roundWatermark, CreateFormWithFooter(scroll, bidFooter));

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
        var (trumpSelectorView, getTrumpIndex) = BuildTrumpIconSelector(() => UpdateTrumpSelectionState(), initialTrumpIndex, currentSession.AllowNoTrump);
        population.Children.Add(trumpSelectorView);
        // Friendly hint label
        var trumpHintLabel = new Label
        {
            Text = Localization.GetString("SelectTrumpHint"),
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Gray,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0),
            IsVisible = true
        };
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

            // Tint the popup background to match the chosen/switched trump color.
            var selectedTrump = hasTrumpSelection
                ? MapSelectionToTrump(getTrumpIndexAccessor!())
                : TrumpSuit.None;
            var popupBg = hasTrumpSelection
                ? FlattenOverSurface(GetTrumpColor(selectedTrump))
                : noTrumpBg;
            bidBackdrop.BackgroundColor = popupBg;
            roundWatermark.TextColor = GetWatermarkColor(selectedTrump);
            ApplyWatermarkText(roundWatermark, currentRoundNumber, selectedTrump);

            // Refresh the dealer's (last) bid field so its color follows the trump selection.
            refreshDealerColor?.Invoke();
        }


        // Dealer bid total warning (if rule is active and dealer is in round)
        var dealerWarningLabel = new Label
        {
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = AppColors.WarningText,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0),
            IsVisible = isBidTotalRuleEnabled && currentRoundNumber >= currentSession.BidTotalRuleStartRound
        };

        // Background color for the dealer's (last) bid field when the bid is valid.
        // Bid fields are always white; only an invalid dealer bid is flagged red.
        Color GetDealerValidColor()
        {
            return AppColors.Surface;
        }

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

                if (otherPlayersBidsSum > currentRoundNumber)
                {
                    // The others already bid more than the round, so the dealer can bid any number.
                    dealerWarningLabel.Text = string.Format(
                        Localization.GetString("DealerCanBidAnyTemplate"),
                        dealerForEntry.Name);

                    if (dealerBidEntry != null)
                    {
                        dealerBidEntry.BackgroundColor = GetDealerValidColor();
                        dealerBidEntry.TextColor = AppColors.TextPrimary;
                    }
                    return;
                }

                var forbiddenText = forbiddenBids.Count > 0
                    ? string.Join(", ", forbiddenBids)
                    : Localization.GetString("None");

                dealerWarningLabel.Text = string.Format(
                    Localization.GetString("DealerCannotBidTemplate"),
                    dealerForEntry.Name,
                    forbiddenText);

                if (dealerBidEntry != null)
                {
                    // Only flag the dealer's bid red once every other player has entered a bid.
                    // While others are still bidding the total can still be made to equal the round,
                    // so the dealer's value isn't necessarily forbidden yet.
                    var othersEntered = bidEntries.Keys
                        .Where(id => id != dealerForEntry.Id)
                        .All(touchedBids.Contains);
                    var dealerBid = int.TryParse(dealerBidEntry.Text, out var parsedBid) ? parsedBid : -1;
                    var isForbidden = othersEntered && forbiddenBids.Contains(dealerBid);
                    dealerBidEntry.BackgroundColor = isForbidden
                        ? Color.FromArgb("#ffcccc")
                        : GetDealerValidColor();
                    dealerBidEntry.TextColor = isForbidden
                        ? Color.FromArgb("#8b0000")
                        : AppColors.TextPrimary;
                }
            }
            else if (dealerBidEntry != null)
            {
                dealerBidEntry.BackgroundColor = GetDealerValidColor();
                dealerBidEntry.TextColor = AppColors.TextPrimary;
            }
        }

        if (dealerWarningLabel.IsVisible)
        {
            population.Children.Add(dealerWarningLabel);
            UpdateDealerWarning();
        }
        refreshDealerColor = UpdateDealerWarning;

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
            // Only warn (red) once every player has entered a bid and the total equals the round,
            // and only when the bid total rule forbids that total for this round.
            // While bids are still being entered the total can still be changed, so keep it green.
            var allEntered = touchedBids.Count == bidEntries.Count;
            var isRuleActive = isBidTotalRuleEnabled && currentRoundNumber >= currentSession.BidTotalRuleStartRound;
            totalBidsLabel.Text = string.Format(Localization.GetString("TotalBidsLabel"), sum, currentRoundNumber);
            totalBidsLabel.TextColor = isRuleActive && allEntered && sum == currentRoundNumber ? AppColors.WarningText : AppColors.SuccessText;
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
            var currentScore = scoreTotals.GetValueOrDefault(player.Id);
            var scoreSuffix = string.Format(Localization.GetString("ScoreInlineLabel"), currentScore);
            var initialBidText = initialBids != null && initialBids.TryGetValue(player.Id, out var restoredBid)
                ? restoredBid.ToString()
                : "0";
            var bidEntry = new Entry
            {
                Keyboard = Keyboard.Numeric,
                Text = initialBidText,
                WidthRequest = 52,
                Placeholder = $"0–{currentRoundNumber}",
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                BackgroundColor = AppColors.Surface
            };
            bidEntry.TextChanged += (s, e) =>
            {
                touchedBids.Add(player.Id);
                UpdateBidTotal();
            };
            bidEntries[player.Id] = bidEntry;
            if (initialBids != null && initialBids.ContainsKey(player.Id))
            {
                touchedBids.Add(player.Id);
            }
            if (isDealer)
            {
                dealerBidEntry = bidEntry;
            }

            var playerLabel = new Label
            {
                Text = isDealer
                    ? $"{player.Name} ({Localization.GetString("DealerLabel")}, {scoreSuffix})"
                    : $"{player.Name} ({scoreSuffix})",
                FontAttributes = isDealer ? FontAttributes.Bold : FontAttributes.None,
                TextColor = isDealer ? AppColors.DealerText : AppColors.TextPrimary,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Fill
            };

            population.Children.Add(CreateStepperRow(playerLabel, bidEntry, 0, currentRoundNumber));
        }

        population.Children.Add(totalBidsLabel);
        UpdateBidTotal();

        doneButton = CreateDialogButton("Ok", isEnabled: false);
        var cancelButton = CreateDialogButton("Cancel");
        var buttonsRow = CreateTwoButtonRow(doneButton, cancelButton);
        bidFooter.Children.Add(trumpHintLabel);
        bidFooter.Children.Add(buttonsRow);
        UpdateTrumpSelectionState();

        // Bids and actuals share one modal page; only its content is swapped between the steps.
        var modal = existingModal ?? new ContentPage();
        modal.Content = bidBackdrop;
        modal.BackgroundColor = noTrumpBg;
        if (existingModal != null)
        {
            App.ApplyBoldToVisualTree(modal, AppSettings.BoldAllText);
        }
        var tcs = new TaskCompletionSource<bool>();

        cancelButton.Clicked += (s, e) => tcs.TrySetResult(false);

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

        if (existingModal == null)
        {
            await Navigation.PushModalAsync(modal, animated: false);
        }
        var proceed = await tcs.Task;

        // Cancelled: return to the scoreboard without starting the round.
        if (!proceed)
        {
            await Navigation.PopModalAsync(animated: false);
            RefreshUI();
            return;
        }

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
            await Navigation.PopModalAsync(animated: false);
            await this.ShowMessageAsync("ErrorTitle", ex.Message);
            return;
        }

        var actuals = await DisplayActualsPopupAsync(currentSession, currentSession.Rounds.Last(), modal);
        if (actuals == null)
        {
            scoreService.CancelRound(currentSession);
            if (Navigation.ModalStack.Contains(modal))
            {
                await DisplayRoundPopupAsync(new Dictionary<Guid, int>(bids), trumpSelectionIndex, modal);
            }
            return;
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
            await this.ShowMessageAsync("ErrorTitle", ex.Message);
        }
    }

    private async Task<Dictionary<Guid, int>?> DisplayActualsPopupAsync(ScoreSession session, RoundEntry round, ContentPage? existingModal = null)
    {
        var isEditing = round.ActualByPlayer.Count > 0;
        var modal = existingModal ?? new ContentPage();
        var trump = round.Trump;
        var orderedPlayers = session.Players.OrderBy(p => p.Order).ToList();
        var dealerIndex = orderedPlayers.FindIndex(p => p.Id == round.DealerPlayerId);
        var entryOrder = orderedPlayers.Skip(dealerIndex + 1).Concat(orderedPlayers.Take(dealerIndex + 1)).ToList();
        var (trumpSymbol, trumpFgColor, trumpBgColor) = GetTrumpDisplayInfo(trump);
        // The trump color is semi-transparent for use over the scoreboard; flatten it over white
        // so the actuals modal page is fully opaque and doesn't reveal the scoreboard behind it.
        var lightBg = FlattenOverSurface(GetTrumpColor(trump));

        var actualEntries = new Dictionary<Guid, Entry>();
        var scrollActuals = new ScrollView { BackgroundColor = Colors.Transparent };
        var popActuals = new StackLayout { Spacing = 14, Padding = new Thickness(16, 12), BackgroundColor = Colors.Transparent };
        scrollActuals.Content = popActuals;

        // Large watermark of the current round number shown behind the actuals form,
        // colored with a dark version of the chosen trump (matching the bidding popup).
        var actualsWatermark = CreateRoundWatermark(round.RoundNumber, trump);
        var actualsFooter = CreateFormFooter();
        var actualsBackdrop = CreateWatermarkBackdrop(lightBg, actualsWatermark, CreateFormWithFooter(scrollActuals, actualsFooter));
        var totalActualsLabel = new Label
        {
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 6, 0, 0)
        };
        Button? doneActuals = null;
        Button? backActuals = null;

        void UpdateActualsTotal()
        {
            var sum = actualEntries.Values.Sum(entry =>
                int.TryParse(entry.Text, out var value) ? value : 0);
            totalActualsLabel.Text = string.Format(Localization.GetString("TotalActualsLabel"), sum, round.RoundNumber);
            totalActualsLabel.TextColor = sum == round.RoundNumber ? AppColors.SuccessText : AppColors.WarningText;
            if (doneActuals != null)
            {
                doneActuals.IsEnabled = sum == round.RoundNumber;
            }
            if (backActuals != null)
            {
                backActuals.IsEnabled = isEditing || sum == 0;
            }
        }

        // Round title
        popActuals.Children.Add(new Label
        {
            Text = string.Format(Localization.GetString("RoundPopupTitle"), round.RoundNumber, session.MaxRounds),
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

        var actualsRefreshes = new List<Action>();
        foreach (var player in entryOrder)
        {
            var bid = round.BidByPlayer.GetValueOrDefault(player.Id);
            var isDealer = player.Id == round.DealerPlayerId;
            var actualEntry = new Entry
            {
                Keyboard = Keyboard.Numeric,
                Text = round.ActualByPlayer.GetValueOrDefault(player.Id).ToString(),
                WidthRequest = 52,
                Placeholder = $"0\u2013{round.RoundNumber}",
                HorizontalTextAlignment = TextAlignment.Center,
                BackgroundColor = AppColors.Surface
            };
            actualEntry.TextChanged += (s, e) => UpdateActualsTotal();
            actualEntries[player.Id] = actualEntry;

            var playerLabel = new Label
            {
                Text = isDealer
                    ? string.Format(Localization.GetString("DealerBidLabelTemplate"), player.Name, Localization.GetString("DealerLabel"), bid)
                    : string.Format(Localization.GetString("PlayerBidLabelTemplate"), player.Name, bid),
                FontAttributes = isDealer ? FontAttributes.Bold : FontAttributes.None,
                TextColor = isDealer ? AppColors.DealerText : AppColors.TextPrimary,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalOptions = LayoutOptions.Fill
            };

            popActuals.Children.Add(CreateStepperRow(playerLabel, actualEntry, 0, round.RoundNumber,
                () => actualEntries.Values.Sum(e => int.TryParse(e.Text, out var n) ? n : 0) >= round.RoundNumber,
                actualsRefreshes));
        }

        actualsRefreshes.ForEach(refresh => refresh());
        popActuals.Children.Add(totalActualsLabel);

        doneActuals = CreateDialogButton("Ok");
        backActuals = CreateDialogButton(isEditing ? "Cancel" : "Back");
        var actualsButtonsRow = CreateTwoButtonRow(backActuals, doneActuals);
        actualsFooter.Children.Add(actualsButtonsRow);
        UpdateActualsTotal();

        // In edit mode, cancellation never returns to bidding or mutates the saved round.
        var tcsActuals = new TaskCompletionSource<bool>();
        backActuals.Clicked += (s, e) => tcsActuals.TrySetResult(false);
        doneActuals.Clicked += async (s, e) =>
        {
            var totalActuals = 0;
            foreach (var player in entryOrder)
            {
                var text = actualEntries[player.Id].Text;
                if (!int.TryParse(text, out var act) || act < 0 || act > round.RoundNumber)
                {
                    await DisplayAlertAsync(
                        Localization.GetString("ErrorTitle"),
                        string.Format(Localization.GetString("ActualsRangeError"), round.RoundNumber),
                        Localization.GetString("Ok"));
                    return;
                }
                totalActuals += act;
            }
            // Total tricks won must equal round number.
            if (totalActuals != round.RoundNumber)
            {
                await DisplayAlertAsync(
                    Localization.GetString("ErrorTitle"),
                    string.Format(Localization.GetString("TotalActualsError"), round.RoundNumber),
                    Localization.GetString("Ok"));
                return;
            }
            tcsActuals.TrySetResult(true);
        };

        modal.BackgroundColor = lightBg;
        modal.Content = actualsBackdrop;
        // Swapping content does not trigger Shell navigation, so apply the bold-text setting here.
        App.ApplyBoldToVisualTree(modal, AppSettings.BoldAllText);
        void OnModalDisappearing(object? sender, EventArgs e) => tcsActuals.TrySetResult(false);
        modal.Disappearing += OnModalDisappearing;
        bool confirmActuals;
        try
        {
            if (existingModal == null)
            {
                await Navigation.PushModalAsync(modal, animated: false);
            }
            confirmActuals = await tcsActuals.Task;
        }
        finally
        {
            modal.Disappearing -= OnModalDisappearing;
        }

        if ((confirmActuals || isEditing) && Navigation.ModalStack.Contains(modal))
        {
            await Navigation.PopModalAsync(animated: false);
        }
        if (!confirmActuals)
            return null;

        var actuals = new Dictionary<Guid, int>();
        foreach (var player in orderedPlayers)
        {
            int.TryParse(actualEntries[player.Id].Text, out var act);
            actuals[player.Id] = act;
        }

        return actuals;
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

    private (View view, Func<int> getSelectedIndex) BuildTrumpIconSelector(Action? onSelectionChanged = null, int initialSelectedIndex = -1, bool allowNoTrump = false)
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

        if (allowNoTrump)
        {
            options = [.. options, mode == TrumpPaletteMode.FourColors
                ? ("●", Colors.White, Colors.LightGray)
                : ("—", Colors.Gray, Colors.White)];
        }

        int selectedIndex = initialSelectedIndex;  // -1 = nothing pre-selected; user must pick
        var buttons = new List<Border>();
        var labels = new List<Label>();

        Color SelectedStroke = AppColors.IsDark ? Colors.White : Colors.Black;
        Color UnselectedStroke = Colors.LightGray;

        void UpdateHighlight()
        {
            for (var i = 0; i < buttons.Count; i++)
            {
                var isSelected = i == selectedIndex;
                buttons[i].Stroke = isSelected ? SelectedStroke : UnselectedStroke;
                buttons[i].StrokeThickness = isSelected ? 4 : 2;
                // Enlarge the chosen trump and fade the others so the selection stands out.
                buttons[i].Scale = isSelected ? 1.2 : 1.0;
                buttons[i].Opacity = selectedIndex < 0 || isSelected ? 1.0 : 0.4;
                if (mode == TrumpPaletteMode.FourColors)
                {
                    labels[i].TextColor = isSelected ? Colors.Black : Colors.White;
                }
            }
            onSelectionChanged?.Invoke();
        }

        var row = new HorizontalStackLayout { Spacing = 14, HorizontalOptions = LayoutOptions.Center, Padding = new Thickness(0, 6) };

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

        if (selectedIndex >= 0)
        {
            UpdateHighlight();
        }

        return (row, () => selectedIndex);
    }

    private static View CreateStepperRow(Label nameLabel, Entry entry, int min, int max, Func<bool>? isPlusBlocked = null, List<Action>? linkedRefreshes = null)
    {
        var minusBtn = new Button
        {
            Text = "−",
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 8,
            Padding = new Thickness(0),
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            BackgroundColor = AppColors.Primary,
            TextColor = Colors.White
        };
        var plusBtn = new Button
        {
            Text = "+",
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 8,
            Padding = new Thickness(0),
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            BackgroundColor = AppColors.Primary,
            TextColor = Colors.White
        };

        void Refresh()
        {
            int.TryParse(entry.Text, out var v);
            minusBtn.IsEnabled = v > min;
            plusBtn.IsEnabled = v < max && !(isPlusBlocked?.Invoke() ?? false);
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
        if (linkedRefreshes != null)
        {
            linkedRefreshes.Add(Refresh);
            entry.TextChanged += (s, e) => linkedRefreshes.ForEach(r => r());
        }
        else
        {
            entry.TextChanged += (s, e) => Refresh();
        }
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
        nameLabel.VerticalOptions = LayoutOptions.Center;
        entry.VerticalOptions = LayoutOptions.Center;
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
                TrumpSuit.Hearts => ("●", Colors.White, Colors.Red),
                TrumpSuit.Diamonds => ("●", Colors.White, Colors.Yellow),
                TrumpSuit.Clubs => ("●", Colors.White, Colors.Green),
                TrumpSuit.Spades => ("●", Colors.White, Colors.Blue),
                TrumpSuit.NoTrump => ("●", Colors.White, Colors.LightGray),
                _ => ("?", Colors.Gray, Colors.White)
            };
        }
        return trump switch
        {
            TrumpSuit.Hearts => ("♥", Colors.Red, Colors.White),
            TrumpSuit.Diamonds => ("♦", Color.FromArgb("#e05000"), Colors.White),
            TrumpSuit.Clubs => ("♣", Colors.DarkGreen, Colors.White),
            TrumpSuit.Spades => ("♠", Colors.DarkBlue, Colors.White),
            TrumpSuit.NoTrump => ("—", Colors.Gray, Colors.White),
            _ => ("?", Colors.Gray, Colors.White)
        };
    }

    private static Color FlattenOverSurface(Color color)
    {
        // In dark mode a softer tint keeps colored text (warnings, dealer) readable.
        var a = color.Alpha * (AppColors.IsDark ? 0.5f : 1f);
        var surface = AppColors.Surface;
        return Color.FromRgb(
            color.Red * a + surface.Red * (1 - a),
            color.Green * a + surface.Green * (1 - a),
            color.Blue * a + surface.Blue * (1 - a));
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
                TrumpSuit.NoTrump => Colors.Gray.WithAlpha(0.2f),
                _ => AppColors.Surface
            };
        }

        // Card suits: red suits get a red background, black suits a blue one.
        return trump switch
        {
            TrumpSuit.Hearts or TrumpSuit.Diamonds => Colors.Red.WithAlpha(0.2f),
            TrumpSuit.Clubs or TrumpSuit.Spades => Colors.Blue.WithAlpha(0.2f),
            TrumpSuit.NoTrump => Colors.Gray.WithAlpha(0.2f),
            _ => AppColors.Surface
        };
    }

    // Dark, opaque version of the trump color used for the round watermark shown
    // behind the bidding form. Falls back to gray when no trump is selected yet.
    private Color GetDarkTrumpColor(TrumpSuit trump) => trumpPaletteService.GetMode() == TrumpPaletteMode.FourColors
        ? trump switch
        {
            TrumpSuit.Hearts => Color.FromArgb("#7a1020"),
            TrumpSuit.Diamonds => Color.FromArgb("#8a6d00"),
            TrumpSuit.Clubs => Color.FromArgb("#0f3d1a"),
            TrumpSuit.Spades => Color.FromArgb("#0f2a5c"),
            _ => Color.FromArgb("#555555")
        }
        : trump switch
        {
            TrumpSuit.Hearts or TrumpSuit.Diamonds => Color.FromArgb("#7a1020"),
            TrumpSuit.Clubs or TrumpSuit.Spades => Color.FromArgb("#0f2a5c"),
            _ => Color.FromArgb("#555555")
        };

    // In card-suit mode the watermark shows the round number followed by the trump symbol.
    private void ApplyWatermarkText(Label watermark, int roundNumber, TrumpSuit trump)
    {
        var showSuit = trumpPaletteService.GetMode() == TrumpPaletteMode.CardSuits && trump is not (TrumpSuit.None or TrumpSuit.NoTrump);
        watermark.Text = showSuit
            ? $"{roundNumber}{GetTrumpDisplayInfo(trump).symbol}"
            : roundNumber.ToString();
        watermark.FontSize = showSuit ? 180 : 260;
    }

    // Semi-transparent dark trump color used for the large round-number watermark.
    private Color GetWatermarkColor(TrumpSuit trump) => AppColors.IsDark
        ? Colors.White.WithAlpha(0.10f)
        : GetDarkTrumpColor(trump).WithAlpha(0.18f);

    // Large round-number watermark shown behind the bidding and actuals forms.
    private Label CreateRoundWatermark(int roundNumber, TrumpSuit trump)
    {
        var watermark = new Label
        {
            FontAttributes = FontAttributes.Bold,
            TextColor = GetWatermarkColor(trump),
            LineBreakMode = LineBreakMode.NoWrap,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            InputTransparent = true
        };
        ApplyWatermarkText(watermark, roundNumber, trump);
        return watermark;
    }

    // Layers the watermark behind the popup content on a tinted backdrop.
    private static Grid CreateWatermarkBackdrop(Color backgroundColor, Label watermark, View content)
    {
        var backdrop = new Grid { BackgroundColor = backgroundColor };
        backdrop.Add(watermark);
        backdrop.Add(content);
        return backdrop;
    }

    // Footer pinned below the scrollable form so the dialog buttons stay at the bottom of the screen.
    private static VerticalStackLayout CreateFormFooter() => new()
    {
        Spacing = 8,
        Padding = new Thickness(16, 8, 16, 12),
        BackgroundColor = Colors.Transparent
    };

    private static Grid CreateFormWithFooter(ScrollView scroll, View footer)
    {
        var layout = new Grid
        {
            RowDefinitions = { new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) }
        };
        layout.Add(scroll, 0, 0);
        layout.Add(footer, 0, 1);
        return layout;
    }

    // Standard full-width dialog button; only the text differs between usages.
    private static Button CreateDialogButton(string textKey, bool isEnabled = true) => new()
    {
        Text = Localization.GetString(textKey),
        IsEnabled = isEnabled,
        HeightRequest = 44,
        VerticalOptions = LayoutOptions.Center,
        HorizontalOptions = LayoutOptions.Fill
    };

    // Two equally sized dialog buttons side by side.
    private static Grid CreateTwoButtonRow(Button left, Button right)
    {
        var row = new Grid
        {
            ColumnSpacing = 8,
            Margin = new Thickness(0, 12, 0, 0),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };
        row.Add(left, 0);
        row.Add(right, 1);
        return row;
    }

    private static TrumpSuit MapSelectionToTrump(int selectedIndex)
    {
        // 0=Hearts, 1=Diamonds, 2=Clubs, 3=Spades, 4=No trump (only offered when the game allows it).
        return selectedIndex switch
        {
            0 => TrumpSuit.Hearts,
            1 => TrumpSuit.Diamonds,
            2 => TrumpSuit.Clubs,
            3 => TrumpSuit.Spades,
            4 => TrumpSuit.NoTrump,
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
            BackgroundColor = AppColors.Surface,
            Stroke = AppColors.Border,
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

        await Shell.Current.GoToAsync($"//{nameof(HighscorePage)}");
    }
}

using Microsoft.Maui.Controls;
using WizardScoreboard.Models;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

public class ScoreBoardPage : ContentPage
{
    private readonly IGroupService groupService;
    private readonly IScoreService scoreService;
    private readonly ITrumpPaletteService trumpPaletteService;
    private ScoreSession? currentSession;

    // Live-updated UI elements
    private Label statusLabel;
    private Grid scoreGrid;
    private Button startButton;
    private Button endButton;
    private Button nextRoundButton;

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

    public ScoreBoardPage(IGroupService groupService, IScoreService scoreService, ITrumpPaletteService trumpPaletteService)
    {
        Title = Localization.GetString("Scoreboard");

        this.groupService    = groupService;
        this.scoreService    = scoreService;
        this.trumpPaletteService = trumpPaletteService;

        statusLabel = new Label
        {
            FontAttributes = FontAttributes.Bold,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };

        scoreGrid = new Grid();

        startButton     = new Button { Text = Localization.GetString("StartGame") };
        endButton       = new Button { Text = Localization.GetString("EndGame") };
        nextRoundButton = new Button { Text = Localization.GetString("NextRound") };

        startButton.Clicked     += async (s, e) => await StartGameAsync();
        endButton.Clicked       += (s, e) => EndGame();
        nextRoundButton.Clicked += async (s, e) => await StartNextRoundAsync();

        var buttonRow = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { startButton, nextRoundButton, endButton }
        };

        Content = new ScrollView
        {
            Content = new StackLayout
            {
                Padding = 12,
                Spacing = 8,
                Children = { statusLabel, scoreGrid, buttonRow }
            }
        };

        RefreshUI();
    }

    private void RefreshUI()
    {
        scoreGrid.Children.Clear();
        scoreGrid.RowDefinitions.Clear();
        scoreGrid.ColumnDefinitions.Clear();

        var hasAvailableGroup = groupService.GetSelectedGroup() != null || groupService.GetGroups().Any();
        var hasSession = currentSession != null;
        var isActiveSession = currentSession?.IsActive == true;
        var hasRoundsRemaining = isActiveSession && currentSession!.CurrentRound < currentSession.MaxRounds;

        startButton.IsEnabled = hasAvailableGroup && !isActiveSession;
        nextRoundButton.IsEnabled = hasRoundsRemaining;
        endButton.IsEnabled = hasSession;

        if (currentSession == null)
        {
            statusLabel.Text = Localization.GetString("NoActiveGame");
            BackgroundColor = Colors.White;
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

        BackgroundColor = GetTrumpColor(currentSession.Trump);

        var players = currentSession.Players.OrderBy(p => p.Order).ToList();

        // ── Column definitions ────────────────────────────────────────────
        // Col 0 = Rnd, Col 1 = Dealer, Col 2..N = players
        scoreGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = 36 });  // Rnd
        scoreGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = 36 });  // Dealer / trump
        foreach (var _ in players)
            scoreGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        // ── Header row ────────────────────────────────────────────────────
        scoreGrid.RowDefinitions.Add(new RowDefinition { Height = 36 });
        AddHeaderCell(scoreGrid, Localization.GetString("RoundHeader"), 0, 0);
        AddHeaderCell(scoreGrid, TrumpHeaderSymbol(), 0, 1);  // column label
        for (var c = 0; c < players.Count; c++)
        {
            AddHeaderCell(scoreGrid, players[c].Name, 0, 2 + c,
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

        // ── Total row (if at least 1 round done) ─────────────────────────
        if (currentSession.Rounds.Any())
        {
            var totalRow = scoreGrid.RowDefinitions.Count;
            scoreGrid.RowDefinitions.Add(new RowDefinition { Height = 32 });

            var totHdr = MakeLabel(Localization.GetString("TotalHeader"),
                bold: true, fontSize: 13, center: true, bg: TotalBg);
            scoreGrid.Add(totHdr, 0, totalRow);
            Grid.SetColumnSpan(totHdr, 2);

            for (var c = 0; c < players.Count; c++)
            {
                var total = runningTotals[players[c].Id];
                AddCell(scoreGrid, total.ToString(), totalRow, 2 + c,
                    bold: true, center: true,
                    color: total >= 0 ? WinFg : LoseFg, bg: TotalBg);
            }
        }
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
            Text = isDealer ? $"★ {text}" : text,
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
            await DisplayAlertAsync(Localization.GetString("InfoTitle"), Localization.GetString("NoActiveGame"), Localization.GetString("Ok"));
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

    private async Task DisplayRoundPopupAsync()
    {
        if (currentSession == null)
            return;

        var currentRoundNumber = currentSession.CurrentRound + 1;
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

        var scroll = new ScrollView();
        var population = new StackLayout { Spacing = 14, Padding = new Thickness(16, 12) };
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
        var (trumpSelectorView, getTrumpIndex) = BuildTrumpIconSelector();
        population.Children.Add(trumpSelectorView);

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
            var playerRow = new HorizontalStackLayout { Spacing = 10 };
            playerRow.Children.Add(new Label
            {
                Text = isDealer
                    ? $"{player.Name} ★ {Localization.GetString("DealerLabel")}"
                    : player.Name,
                FontAttributes = isDealer ? FontAttributes.Bold : FontAttributes.None,
                TextColor = isDealer ? Color.FromArgb("#b26a00") : Colors.Black,
                VerticalTextAlignment = TextAlignment.Center,
                WidthRequest = 140
            });
            var bidEntry = new Entry
            {
                Keyboard = Keyboard.Numeric,
                Text = "0",
                WidthRequest = 80,
                Placeholder = $"0–{currentRoundNumber}"
            };
            bidEntry.TextChanged += (s, e) => UpdateBidTotal();
            bidEntries[player.Id] = bidEntry;
            playerRow.Children.Add(bidEntry);
            population.Children.Add(playerRow);
        }

        population.Children.Add(totalBidsLabel);
        UpdateBidTotal();

        var doneButton = new Button
        {
            Text = Localization.GetString("Ok"),
            Margin = new Thickness(0, 12, 0, 0)
        };
        population.Children.Add(doneButton);

        var modal = new ContentPage { Content = scroll };
        var tcs = new TaskCompletionSource<bool>();

        doneButton.Clicked += async (s, e) =>
        {
            // Trump must be selected.
            if (getTrumpIndex() < 0)
            {
                await DisplayAlertAsync(
                    Localization.GetString("ErrorTitle"),
                    Localization.GetString("TrumpRequiredError"),
                    Localization.GetString("Ok"));
                return;
            }
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
            if (totalBids == currentRoundNumber)
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
        var actualEntries = new Dictionary<Guid, Entry>();
        var scrollActuals = new ScrollView();
        var popActuals = new StackLayout { Spacing = 14, Padding = new Thickness(16, 12) };
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

        popActuals.Children.Add(new Label
        {
            Text = string.Format(Localization.GetString("RoundPopupTitle"), currentSession.CurrentRound, currentSession.MaxRounds),
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        });

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
            var row = new HorizontalStackLayout { Spacing = 10 };
            row.Children.Add(new Label
            {
                Text = isDealer
                    ? $"{player.Name} ★ {Localization.GetString("DealerLabel")} (bid: {bid})"
                    : $"{player.Name} (bid: {bid})",
                FontAttributes = isDealer ? FontAttributes.Bold : FontAttributes.None,
                TextColor = isDealer ? Color.FromArgb("#b26a00") : Colors.Black,
                VerticalTextAlignment = TextAlignment.Center,
                WidthRequest = 160
            });
            var actualEntry = new Entry
            {
                Keyboard = Keyboard.Numeric,
                Text = "0",
                WidthRequest = 80,
                Placeholder = $"0\u2013{currentSession.CurrentRound}"
            };
            actualEntry.TextChanged += (s, e) => UpdateActualsTotal();
            actualEntries[player.Id] = actualEntry;
            row.Children.Add(actualEntry);
            popActuals.Children.Add(row);
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

        await Navigation.PushModalAsync(new ContentPage { Content = scrollActuals });
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
            RefreshUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(Localization.GetString("ErrorTitle"), ex.Message, Localization.GetString("Ok"));
        }
    }

    private (View view, Func<int> getSelectedIndex) BuildTrumpIconSelector()
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
}

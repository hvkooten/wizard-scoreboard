using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Linq;
using WizardScoreboard.Models;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

public class SettingsPage : ContentPage
{
    private static readonly (string DisplayName, string CultureName)[] LanguageOptions =
    {
        ("Nederlands", "nl-NL"),
        ("English", "en-US"),
        ("Deutsch", "de-DE"),
        ("Espanol", "es-ES"),
        ("Francais", "fr-FR")
    };

    private readonly IGroupService groupService;
    private readonly ITrumpPaletteService trumpPaletteService;
    private readonly Picker languagePicker;
    private readonly Picker trumpPalettePicker;
    private readonly HorizontalStackLayout trumpPalettePreviewLayout;
    private readonly Picker bidTotalRulePicker;
    private readonly Picker groupPicker;
    private int selectedPlayerCount = 6;
    private readonly List<Button> playerCountButtons = new();
    private readonly HorizontalStackLayout playerCountLayout;
    private readonly Entry groupNameEntry;
    private readonly VerticalStackLayout playerNamesLayout;
    private readonly List<Entry> playerNameEntries = new();
    private readonly List<string> allPlayerNames = new() { "", "", "", "", "", "" };
    private bool _skipFlushOnNextRebuild;
    private Guid? editingGroupId;
    private bool groupNameUserEdited;
    private readonly Button createGroupButton;
    private readonly CollectionView groupListView;
    private int bidTotalRuleStartRoundValue = 6;

    public SettingsPage(IGroupService groupService, ITrumpPaletteService trumpPaletteService)
    {
        Title = $"{Localization.GetString("Settings")} · Wizard";

        this.groupService = groupService;
        this.trumpPaletteService = trumpPaletteService;

        languagePicker = new Picker { Title = Localization.GetString("SelectLanguage") };
        foreach (var option in LanguageOptions)
        {
            languagePicker.Items.Add(option.DisplayName);
        }

        var selectedCulture = Localization.ResolveSupportedCulture(CultureInfo.CurrentUICulture.Name);
        var selectedLanguageIndex = Array.FindIndex(
            LanguageOptions,
            option => string.Equals(option.CultureName, selectedCulture, StringComparison.OrdinalIgnoreCase));
        if (selectedLanguageIndex < 0)
        {
            selectedLanguageIndex = 0;
        }

        languagePicker.SelectedIndex = selectedLanguageIndex;

        languagePicker.SelectedIndexChanged += (s, e) =>
        {
            if (languagePicker.SelectedIndex >= 0)
            {
                var selectedOption = LanguageOptions[languagePicker.SelectedIndex];
                Localization.SetCulture(selectedOption.CultureName);
                var shell = Application.Current?.Handler?.MauiContext?.Services.GetService<AppShell>();
                var window = Application.Current?.Windows.FirstOrDefault();
                if (shell != null && window != null)
                {
                    window.Page = shell;
                }
            }
        };

        trumpPalettePicker = new Picker { Title = string.Empty };
        trumpPalettePreviewLayout = new HorizontalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center
        };
        trumpPalettePicker.Items.Add(Localization.GetString("TrumpPaletteCards"));
        trumpPalettePicker.Items.Add(Localization.GetString("TrumpPaletteColors"));
        trumpPalettePicker.SelectedIndex = trumpPaletteService.GetMode() == TrumpPaletteMode.FourColors ? 1 : 0;
        trumpPalettePicker.SelectedIndexChanged += (s, e) =>
        {
            var mode = trumpPalettePicker.SelectedIndex == 1 ? TrumpPaletteMode.FourColors : TrumpPaletteMode.CardSuits;
            trumpPaletteService.SetMode(mode);
            UpdateTrumpPalettePreview(mode);
        };

        var trumpStyleHeaderRow = new HorizontalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label
                {
                    Text = Localization.GetString("TrumpColorStyleTitle"),
                    FontAttributes = FontAttributes.Bold,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalOptions = LayoutOptions.Start
                },
                trumpPalettePreviewLayout
            }
        };
        UpdateTrumpPalettePreview(trumpPaletteService.GetMode());

        // Bid total rule start round setting
        bidTotalRulePicker = new Picker { Title = Localization.GetString("BidTotalRuleStartRound") };
        bidTotalRulePicker.Items.Add(Localization.GetString("Disabled"));
        for (var round = 1; round <= 13; round++)
        {
            bidTotalRulePicker.Items.Add(round.ToString());
        }
        bidTotalRulePicker.SelectedIndex = bidTotalRuleStartRoundValue;
        bidTotalRulePicker.SelectedIndexChanged += (s, e) =>
        {
            if (bidTotalRulePicker.SelectedIndex >= 0)
            {
                bidTotalRuleStartRoundValue = bidTotalRulePicker.SelectedIndex;
            }
        };

        groupNameEntry = new Entry { Placeholder = Localization.GetString("CreatorGroup") };
        groupNameEntry.TextChanged += (s, e) =>
        {
            // Mark as user-edited only when the change comes from the user, not from code.
            if (!_suppressGroupNameFlag)
            {
                groupNameUserEdited = true;
            }
        };

        groupPicker = new Picker { Title = "Saved groups" };
        groupPicker.SelectedIndexChanged += GroupPicker_SelectedIndexChanged;

        playerCountLayout = new HorizontalStackLayout { Spacing = 8 };
        foreach (var count in new[] { 3, 4, 5, 6 })
        {
            var btn = new Button
            {
                Text = count.ToString(),
                WidthRequest = 52,
                HeightRequest = 44,
                CornerRadius = 8,
                FontSize = 16
            };
            var capturedCount = count;
            btn.Clicked += (s, e) => SetPlayerCount(capturedCount);
            playerCountButtons.Add(btn);
            playerCountLayout.Children.Add(btn);
        }
        ApplyPlayerCountButtonStyles();

        playerNamesLayout = new VerticalStackLayout
        {
            Spacing = 8
        };
        RebuildPlayerNameInputs();

        createGroupButton = new Button { Text = Localization.GetString("CreatorGroup") };
        createGroupButton.Clicked += CreateGroupButton_Clicked;

        groupListView = new CollectionView();

        Content = new ScrollView
        {
            Content = new StackLayout
            {
                Padding = 20,
                Spacing = 15,
                Children =
                {
                    languagePicker,
                    trumpStyleHeaderRow,
                    trumpPalettePicker,
                    bidTotalRulePicker,
                    groupPicker,
                    groupNameEntry,
                    playerCountLayout,
                    playerNamesLayout,
                    createGroupButton,
                    groupListView
                }
            }
        };

        RefreshGroups();
    }

    private void RefreshGroups()
    {
        var groups = groupService.GetGroups().OrderBy(g => g.CreatedAt).ToList();

        groupPicker.Items.Clear();
        groupPicker.Items.Add("New group");
        foreach (var group in groups)
        {
            groupPicker.Items.Add(group.Name);
        }

        var selectedGroup = groupService.GetSelectedGroup();
        if (selectedGroup != null)
        {
            var selectedIndex = groups.FindIndex(g => g.Id == selectedGroup.Id);
            groupPicker.SelectedIndex = selectedIndex >= 0 ? selectedIndex + 1 : 0;
        }
        else
        {
            groupPicker.SelectedIndex = 0;
        }

        groupListView.ItemsSource = groups.Select(g => g.Name).ToList();
    }

    private async void CreateGroupButton_Clicked(object? sender, EventArgs e)
    {
        var groupName = groupNameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(groupName))
        {
            await DisplayAlertAsync(Localization.GetString("ErrorTitle"), Localization.GetString("GroupNameRequired"), Localization.GetString("Ok"));
            return;
        }

        try
        {
            // Flush visible entries into the backing cache first so nothing is lost.
            FlushVisibleEntriesToCache();

            // Build player list from ALL non-empty cached names (may exceed visible count).
            var players = new List<Player>();
            for (var i = 0; i < allPlayerNames.Count; i++)
            {
                var name = allPlayerNames[i].Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    players.Add(new Player { Name = name, Order = i });
                }
            }

            if (players.Count < 3 || players.Count > 6)
            {
                await DisplayAlertAsync(
                    Localization.GetString("ErrorTitle"),
                    Localization.GetString("GroupNameRequired"),
                    Localization.GetString("Ok"));
                return;
            }

            if (editingGroupId.HasValue)
            {
                var existing = groupService.GetGroup(editingGroupId.Value);
                if (existing != null)
                {
                    existing.Name = groupName;
                    existing.Players = players;
                    existing.BidTotalRuleStartRound = bidTotalRuleStartRoundValue;
                    groupService.UpdateGroup(existing);
                    groupService.SetSelectedGroup(existing.Id);
                }
            }
            else
            {
                var created = groupService.CreateGroup(groupName, players);
                created.BidTotalRuleStartRound = bidTotalRuleStartRoundValue;
                groupService.UpdateGroup(created);
                groupService.SetSelectedGroup(created.Id);
            }

            RefreshGroups();
            groupNameUserEdited = false;
            var successMsg = editingGroupId.HasValue
                ? Localization.GetString("GroupUpdated")
                : Localization.GetString("GroupCreated");
            await DisplayAlertAsync(Localization.GetString("SuccessTitle"), successMsg, Localization.GetString("Ok"));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(Localization.GetString("ErrorTitle"), ex.Message, Localization.GetString("Ok"));
        }
    }

    private static View CreateColorIcon(Color color)
    {
        return new Border
        {
            WidthRequest = 28,
            HeightRequest = 28,
            BackgroundColor = color,
            Stroke = Colors.Black,
            StrokeThickness = 1,
            Padding = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 14 }
        };
    }

    private static View CreateSuitIcon(string symbol, Color color)
    {
        return new Border
        {
            WidthRequest = 28,
            HeightRequest = 28,
            BackgroundColor = Colors.White,
            Stroke = Colors.Black,
            StrokeThickness = 1,
            Padding = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Content = new Label
            {
                Text = symbol,
                TextColor = color,
                FontAttributes = FontAttributes.Bold,
                FontSize = 15,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    private void UpdateTrumpPalettePreview(TrumpPaletteMode mode)
    {
        trumpPalettePreviewLayout.Children.Clear();

        if (mode == TrumpPaletteMode.FourColors)
        {
            trumpPalettePreviewLayout.Children.Add(CreateColorIcon(Colors.Red));
            trumpPalettePreviewLayout.Children.Add(CreateColorIcon(Colors.Yellow));
            trumpPalettePreviewLayout.Children.Add(CreateColorIcon(Colors.Green));
            trumpPalettePreviewLayout.Children.Add(CreateColorIcon(Colors.Blue));
            return;
        }

        trumpPalettePreviewLayout.Children.Add(CreateSuitIcon("♥", Colors.Red));
        trumpPalettePreviewLayout.Children.Add(CreateSuitIcon("♦", Color.FromArgb("#e05000")));
        trumpPalettePreviewLayout.Children.Add(CreateSuitIcon("♣", Colors.DarkGreen));
        trumpPalettePreviewLayout.Children.Add(CreateSuitIcon("♠", Colors.DarkBlue));
    }

    private void RebuildPlayerNameInputs()
    {
        RebuildPlayerNameInputs(null);
    }

    private void FlushVisibleEntriesToCache()
    {
        for (var k = 0; k < playerNameEntries.Count && k < allPlayerNames.Count; k++)
        {
            if (!string.IsNullOrWhiteSpace(playerNameEntries[k].Text))
            {
                allPlayerNames[k] = playerNameEntries[k].Text!;
            }
        }
    }

    private void RebuildPlayerNameInputs(List<string>? namesOverride)
    {
        if (!_skipFlushOnNextRebuild)
        {
            FlushVisibleEntriesToCache();
        }
        _skipFlushOnNextRebuild = false;

        if (namesOverride != null)
        {
            for (var k = 0; k < namesOverride.Count && k < allPlayerNames.Count; k++)
            {
                allPlayerNames[k] = namesOverride[k];
            }
            // Clear slots beyond the new group size so stale names from a larger group don't persist.
            for (var k = namesOverride.Count; k < allPlayerNames.Count; k++)
            {
                allPlayerNames[k] = string.Empty;
            }
        }

        playerNameEntries.Clear();
        playerNamesLayout.Children.Clear();

        // Use backing cache as source so names survive count reductions.
        var sourceNames = allPlayerNames;

        var selectedPlayerCount = this.selectedPlayerCount;

        for (var i = 0; i < selectedPlayerCount; i++)
        {
            var previous = i < sourceNames.Count ? sourceNames[i] : null;
            var entry = new Entry
            {
                Placeholder = $"Speler {i + 1}",
                Text = string.IsNullOrWhiteSpace(previous)
                    ? string.Format(Localization.GetString("PlayerPlaceholder"), i + 1)
                    : previous
            };

            var rowIndex = i;

            var upButton = new Button
            {
                Text = "▲",
                WidthRequest = 36,
                HeightRequest = 36,
                Padding = new Thickness(0),
                FontSize = 14,
                IsEnabled = i > 0
            };
            upButton.Clicked += (s, e) => MovePlayerInput(rowIndex, rowIndex - 1);

            var downButton = new Button
            {
                Text = "▼",
                WidthRequest = 36,
                HeightRequest = 36,
                Padding = new Thickness(0),
                FontSize = 14,
                IsEnabled = i < selectedPlayerCount - 1
            };
            downButton.Clicked += (s, e) => MovePlayerInput(rowIndex, rowIndex + 1);

            var deleteButton = new Button
            {
                Text = "✕",
                WidthRequest = 36,
                HeightRequest = 36,
                Padding = new Thickness(0),
                FontSize = 14,
                BackgroundColor = Colors.LightCoral,
                TextColor = Colors.White,
                IsEnabled = selectedPlayerCount > 3
            };
            deleteButton.Clicked += (s, e) => DeletePlayerInput(rowIndex);

            var rowLayout = new HorizontalStackLayout
            {
                Spacing = 6,
                Children = { upButton, downButton, deleteButton, entry }
            };

            var rowBorder = new Border
            {
                Padding = new Thickness(8, 4),
                Stroke = Colors.LightGray,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 6 },
                Content = rowLayout
            };

            entry.TextChanged += (s, e) => UpdateGroupNameFromPlayers();

            playerNameEntries.Add(entry);
            playerNamesLayout.Children.Add(rowBorder);
        }
    }

    private bool _suppressGroupNameFlag;

    private void SetGroupNameFromCode(string name)
    {
        _suppressGroupNameFlag = true;
        groupNameEntry.Text = name;
        _suppressGroupNameFlag = false;
    }

    private void UpdateGroupNameFromPlayers()
    {
        if (groupNameUserEdited)
        {
            return;
        }

        var names = playerNameEntries
            .Select(e => e.Text?.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        if (names.Count > 0)
        {
            SetGroupNameFromCode(string.Join(" & ", names));
        }
    }

    private void MovePlayerInput(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex || fromIndex < 0 || toIndex < 0 || fromIndex >= playerNameEntries.Count || toIndex >= playerNameEntries.Count)
        {
            return;
        }

        // Flush visible text into cache, swap directly in cache, then rebuild
        // without a second flush overwriting the swap.
        FlushVisibleEntriesToCache();
        (allPlayerNames[fromIndex], allPlayerNames[toIndex]) = (allPlayerNames[toIndex], allPlayerNames[fromIndex]);
        _skipFlushOnNextRebuild = true;
        RebuildPlayerNameInputs(null);
    }

    private void DeletePlayerInput(int deleteIndex)
    {
        if (deleteIndex < 0 || deleteIndex >= selectedPlayerCount || selectedPlayerCount <= 3)
        {
            return;
        }

        var previousCount = selectedPlayerCount;

        // Flush visible text into cache
        FlushVisibleEntriesToCache();

        // Shift all names after deleteIndex down by one
        for (var i = deleteIndex; i < allPlayerNames.Count - 1; i++)
        {
            allPlayerNames[i] = allPlayerNames[i + 1];
        }
        allPlayerNames[allPlayerNames.Count - 1] = string.Empty;

        // Reduce player count and rebuild
        selectedPlayerCount--;

        // Keep default bid-rule round aligned with group size after delete.
        if (bidTotalRuleStartRoundValue == previousCount)
        {
            bidTotalRuleStartRoundValue = selectedPlayerCount;
            if (bidTotalRulePicker.SelectedIndex != bidTotalRuleStartRoundValue)
            {
                bidTotalRulePicker.SelectedIndex = bidTotalRuleStartRoundValue;
            }
        }

        ApplyPlayerCountButtonStyles();
        _skipFlushOnNextRebuild = true;
        RebuildPlayerNameInputs(null);
        UpdateGroupNameFromPlayers();
    }

    private void SetPlayerCount(int count)
    {
        var previousCount = selectedPlayerCount;
        selectedPlayerCount = count;

        // Keep default bid-rule round aligned with group size.
        if (bidTotalRuleStartRoundValue == previousCount)
        {
            bidTotalRuleStartRoundValue = count;
            if (bidTotalRulePicker.SelectedIndex != bidTotalRuleStartRoundValue)
            {
                bidTotalRulePicker.SelectedIndex = bidTotalRuleStartRoundValue;
            }
        }

        ApplyPlayerCountButtonStyles();
        RebuildPlayerNameInputs();
    }

    private void ApplyPlayerCountButtonStyles()
    {
        foreach (var btn in playerCountButtons)
        {
            var isSelected = btn.Text == selectedPlayerCount.ToString();
            btn.BackgroundColor = isSelected ? Color.FromArgb("#1a3a5c") : Color.FromArgb("#e0e8f0");
            btn.TextColor = isSelected ? Colors.White : Color.FromArgb("#1a3a5c");
        }
    }

    private void GroupPicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var groups = groupService.GetGroups().OrderBy(g => g.CreatedAt).ToList();
        if (groupPicker.SelectedIndex <= 0)
        {
            editingGroupId = null;
            groupNameUserEdited = false;
            createGroupButton.Text = Localization.GetString("CreatorGroup");
            SetGroupNameFromCode(string.Empty);
            SetPlayerCount(6);
            bidTotalRuleStartRoundValue = selectedPlayerCount;
            bidTotalRulePicker.SelectedIndex = bidTotalRuleStartRoundValue;
            for (var k = 0; k < allPlayerNames.Count; k++) allPlayerNames[k] = string.Empty;
            RebuildPlayerNameInputs();
            return;
        }

        var selectedIndex = groupPicker.SelectedIndex - 1;
        if (selectedIndex < 0 || selectedIndex >= groups.Count)
        {
            return;
        }

        var selected = groups[selectedIndex];
        editingGroupId = selected.Id;
        groupNameUserEdited = true;  // Existing group has a real name — don't overwrite it.
        groupService.SetSelectedGroup(selected.Id);
        createGroupButton.Text = "Save group";
        SetGroupNameFromCode(selected.Name);

        SetPlayerCount(selected.Players.Count);
        bidTotalRuleStartRoundValue = selected.BidTotalRuleStartRound is >= 0 and <= 13
            ? selected.BidTotalRuleStartRound
            : selected.Players.Count;
        bidTotalRulePicker.SelectedIndex = bidTotalRuleStartRoundValue;

        var orderedNames = selected.Players.OrderBy(p => p.Order).Select(p => p.Name).ToList();
        RebuildPlayerNameInputs(orderedNames);
    }
}

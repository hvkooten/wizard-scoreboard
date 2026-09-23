using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System.Linq;
using WizardScoreboard.Models;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

public class GroupsPage : ContentPage
{
    private readonly IGroupService groupService;
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
    private bool _suppressGroupNameFlag;
    private readonly Button createGroupButton;
    private readonly CollectionView groupListView;
    private int bidTotalRuleStartRoundValue = AppSettings.DefaultBidTotalRuleStartRound;

    public GroupsPage(IGroupService groupService)
    {
        PageTitleHelper.Apply(this, Localization.GetString("Groups"));

        this.groupService = groupService;

        // Bid total rule start round setting (per group).
        bidTotalRulePicker = new Picker { Title = Localization.GetString("BidTotalRuleStartRound") };
        bidTotalRulePicker.Items.Add(Localization.GetString("Disabled"));
        bidTotalRulePicker.Items.Add(Localization.GetString("Player Count"));
        bidTotalRulePicker.Items.Add(Localization.GetString("DoublePlayerCount"));
        for (var round = 1; round <= 13; round++)
        {
            bidTotalRulePicker.Items.Add(round.ToString());
        }
        bidTotalRulePicker.SelectedIndex = BidTotalRulePicker.IndexFromValue(bidTotalRuleStartRoundValue);
        bidTotalRulePicker.SelectedIndexChanged += (s, e) =>
        {
            if (bidTotalRulePicker.SelectedIndex >= 0)
            {
                bidTotalRuleStartRoundValue = BidTotalRulePicker.ValueFromIndex(bidTotalRulePicker.SelectedIndex);
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

        groupPicker = new Picker { Title = Localization.GetString("SelectGroup") };
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

        groupListView = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemTemplate = new DataTemplate(() =>
            {
                var row = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    ColumnSpacing = 8,
                    Padding = new Thickness(8, 4)
                };

                var nameLabel = new Label
                {
                    VerticalTextAlignment = TextAlignment.Center,
                    FontSize = 14
                };
                nameLabel.SetBinding(Label.TextProperty, nameof(Group.Name));

                var deleteButton = new Button
                {
                    Text = "🗑",
                    WidthRequest = 44,
                    HeightRequest = 36,
                    Padding = new Thickness(0),
                    BackgroundColor = Color.FromArgb("#fde8e8"),
                    TextColor = Color.FromArgb("#a32020"),
                    CornerRadius = 8
                };
                deleteButton.Clicked += async (s, e) =>
                {
                    if (deleteButton.BindingContext is Group group)
                    {
                        await ConfirmDeleteGroupAsync(group);
                    }
                };

                row.Add(nameLabel, 0, 0);
                row.Add(deleteButton, 1, 0);

                return row;
            })
        };

        Content = new ScrollView
        {
            Content = new StackLayout
            {
                Padding = 20,
                Spacing = 15,
                Children =
                {
                    groupPicker,
                    groupNameEntry,
                    bidTotalRulePicker,
                    playerCountLayout,
                    playerNamesLayout,
                    createGroupButton,
                    groupListView
                }
            }
        };

        RefreshGroups();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshGroups();
    }

    private void RefreshGroups()
    {
        var groups = groupService.GetGroups().OrderBy(g => g.CreatedAt).ToList();

        groupPicker.Items.Clear();
        groupPicker.Items.Add(Localization.GetString("NewGroup"));
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

        groupListView.ItemsSource = groups;
    }

    private async Task ConfirmDeleteGroupAsync(Group group)
    {
        var confirm = await DisplayAlertAsync(
            Localization.GetString("DeleteGroupConfirmTitle"),
            string.Format(Localization.GetString("DeleteGroupConfirmMessage"), group.Name),
            Localization.GetString("Yes"),
            Localization.GetString("No"));

        if (!confirm)
        {
            return;
        }

        groupService.DeleteGroup(group.Id);

        if (editingGroupId == group.Id)
        {
            editingGroupId = null;
        }

        RefreshGroups();
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

            // Persist only currently selected visible players for both create and edit.
            var players = new List<Player>();
            for (var i = 0; i < selectedPlayerCount; i++)
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

            entry.Focused += (s, e) =>
            {
                if (s is not Entry focusedEntry)
                {
                    return;
                }

                focusedEntry.Dispatcher.Dispatch(() =>
                {
                    var textLength = focusedEntry.Text?.Length ?? 0;
                    focusedEntry.CursorPosition = 0;
                    focusedEntry.SelectionLength = textLength;
                });
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
            var alignedIndex = BidTotalRulePicker.IndexFromValue(bidTotalRuleStartRoundValue);
            if (bidTotalRulePicker.SelectedIndex != alignedIndex)
            {
                bidTotalRulePicker.SelectedIndex = alignedIndex;
            }
        }

        ApplyPlayerCountButtonStyles();
        _skipFlushOnNextRebuild = true;
        RebuildPlayerNameInputs(null);
        UpdateGroupNameFromPlayers();
    }

    private void SetPlayerCount(int count, bool updateGroupName = true)
    {
        var previousCount = selectedPlayerCount;
        selectedPlayerCount = count;

        // Keep default bid-rule round aligned with group size.
        if (bidTotalRuleStartRoundValue == previousCount)
        {
            bidTotalRuleStartRoundValue = count;
            var alignedIndex = BidTotalRulePicker.IndexFromValue(bidTotalRuleStartRoundValue);
            if (bidTotalRulePicker.SelectedIndex != alignedIndex)
            {
                bidTotalRulePicker.SelectedIndex = alignedIndex;
            }
        }

        ApplyPlayerCountButtonStyles();
        RebuildPlayerNameInputs();

        if (updateGroupName)
        {
            UpdateGroupNameFromPlayers();
        }
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
            SetPlayerCount(6, updateGroupName: false);
            bidTotalRuleStartRoundValue = AppSettings.DefaultBidTotalRuleStartRound;
            bidTotalRulePicker.SelectedIndex = BidTotalRulePicker.IndexFromValue(bidTotalRuleStartRoundValue);
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
        createGroupButton.Text = Localization.GetString("SaveGroup");
        SetGroupNameFromCode(selected.Name);

        SetPlayerCount(selected.Players.Count, updateGroupName: false);
        bidTotalRuleStartRoundValue = selected.BidTotalRuleStartRound is (>= 0 and <= 13) or Group.DoublePlayerCountRule
            ? selected.BidTotalRuleStartRound
            : Group.PlayerCountRule;
        bidTotalRulePicker.SelectedIndex = BidTotalRulePicker.IndexFromValue(bidTotalRuleStartRoundValue);

        var orderedNames = selected.Players.OrderBy(p => p.Order).Select(p => p.Name).ToList();
        RebuildPlayerNameInputs(orderedNames);
    }
}

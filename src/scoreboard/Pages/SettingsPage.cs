using Microsoft.Maui.Controls;
using WizardScoreboard.Models;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

public class SettingsPage : ContentPage
{
    private readonly IGroupService groupService;
    private readonly Picker languagePicker;
    private readonly Entry groupNameEntry;
    private readonly Button createGroupButton;
    private readonly ListView groupListView;

    public SettingsPage()
    {
        Title = Localization.GetString("Settings");

        groupService = new GroupService();

        languagePicker = new Picker { Title = Localization.GetString("SelectLanguage") };
        languagePicker.Items.Add("nl-NL");
        languagePicker.Items.Add("en-US");
        languagePicker.Items.Add("de-DE");
        languagePicker.Items.Add("es-ES");
        languagePicker.Items.Add("fr-FR");
        languagePicker.SelectedIndexChanged += (s, e) =>
        {
            if (languagePicker.SelectedItem is string culture)
            {
                Localization.SetCulture(culture);
                Application.Current.MainPage = new AppShell();
            }
        };

        groupNameEntry = new Entry { Placeholder = Localization.GetString("CreatorGroup") };

        createGroupButton = new Button { Text = Localization.GetString("CreatorGroup") };
        createGroupButton.Clicked += CreateGroupButton_Clicked;

        groupListView = new ListView();

        Content = new ScrollView
        {
            Content = new StackLayout
            {
                Padding = 20,
                Spacing = 15,
                Children =
                {
                    languagePicker,
                    groupNameEntry,
                    createGroupButton,
                    groupListView
                }
            }
        };

        RefreshGroups();
    }

    private void RefreshGroups()
    {
        groupListView.ItemsSource = groupService.GetGroups().Select(g => g.Name).ToList();
    }

    private async void CreateGroupButton_Clicked(object sender, EventArgs e)
    {
        var groupName = groupNameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(groupName))
        {
            await DisplayAlert("Error", "Group name is required.", "OK");
            return;
        }

        try
        {
            var defaultPlayers = new List<Player>
            {
                new Player { Name = "Player 1", Order = 0 },
                new Player { Name = "Player 2", Order = 1 },
                new Player { Name = "Player 3", Order = 2 }
            };

            groupService.CreateGroup(groupName, defaultPlayers);
            RefreshGroups();
            await DisplayAlert("Success", "Group created", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
}

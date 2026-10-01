using Microsoft.Maui.Controls.Shapes;
using System.Globalization;
using WizardScoreboard.Models;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

public class SettingsPage : ContentPage
{
    private static readonly (string DisplayName, string CultureName)[] LanguageOptions =
    {
        ("🇳🇱 Nederlands", "nl-NL"),
        ("🇬🇧 English", "en-US"),
        ("🇩🇪 Deutsch", "de-DE"),
        ("🇪🇸 Espanol", "es-ES"),
        ("🇫🇷 Francais", "fr-FR")
    };

    private static readonly AppTheme[] ThemeOptions = { AppTheme.Unspecified, AppTheme.Light, AppTheme.Dark };

    private readonly ITrumpPaletteService trumpPaletteService;
    private readonly IScreenWakeService screenWakeService;
    private readonly Picker languagePicker;
    private readonly Picker trumpPalettePicker;
    private readonly HorizontalStackLayout trumpPalettePreviewLayout;
    private readonly Picker bidTotalRulePicker;

    public SettingsPage(ITrumpPaletteService trumpPaletteService, IScreenWakeService screenWakeService)
    {
        PageTitleHelper.Apply(this, Localization.GetString("Settings"));

        this.trumpPaletteService = trumpPaletteService;
        this.screenWakeService = screenWakeService;

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
                AppSettings.Language = Localization.SetCulture(selectedOption.CultureName);
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

        var trumpStyleHeaderRow = CreateSettingRow(Localization.GetString("TrumpColorStyleTitle"), trumpPalettePreviewLayout, boldLabel: true);
        UpdateTrumpPalettePreview(trumpPaletteService.GetMode());

        // Default bid total rule start round applied when creating a new group.
        bidTotalRulePicker = new Picker { Title = Localization.GetString("DefaultBidTotalRuleStartRound") };
        // The default is not tied to a player count, so offer every possible round.
        BidTotalRulePicker.PopulateItems(bidTotalRulePicker, Group.MaxRoundLimit);
        bidTotalRulePicker.SelectedIndex = BidTotalRulePicker.IndexFromValue(AppSettings.DefaultBidTotalRuleStartRound);
        bidTotalRulePicker.SelectedIndexChanged += (s, e) =>
        {
            if (bidTotalRulePicker.SelectedIndex >= 0)
            {
                AppSettings.DefaultBidTotalRuleStartRound = BidTotalRulePicker.ValueFromIndex(bidTotalRulePicker.SelectedIndex);
            }
        };

        var keepScreenAwakeSwitch = new Switch
        {
            IsToggled = AppSettings.KeepScreenAwakeDuringGame,
            VerticalOptions = LayoutOptions.Center
        };
        keepScreenAwakeSwitch.Toggled += (s, e) =>
        {
            AppSettings.KeepScreenAwakeDuringGame = e.Value;
            screenWakeService.Refresh();
        };

        var keepScreenAwakeRow = CreateSettingRow(Localization.GetString("KeepScreenAwake"), keepScreenAwakeSwitch);

        var boldAllTextSwitch = new Switch
        {
            IsToggled = AppSettings.BoldAllText,
            VerticalOptions = LayoutOptions.Center
        };
        boldAllTextSwitch.Toggled += (s, e) =>
        {
            AppSettings.BoldAllText = e.Value;
            App.ApplyGlobalTextStyle();
        };

        var boldAllTextRow = CreateSettingRow(Localization.GetString("BoldAllText"), boldAllTextSwitch);

        var themePicker = new Picker();
        themePicker.Items.Add(Localization.GetString("AppThemeSystem"));
        themePicker.Items.Add(Localization.GetString("AppThemeLight"));
        themePicker.Items.Add(Localization.GetString("AppThemeDark"));
        themePicker.SelectedIndex = Math.Max(0, Array.IndexOf(ThemeOptions, AppSettings.Theme));
        themePicker.SelectedIndexChanged += (s, e) =>
        {
            if (themePicker.SelectedIndex >= 0)
            {
                App.ApplyTheme(ThemeOptions[themePicker.SelectedIndex]);
            }
        };

        Content = new ScrollView
        {
            Content = new StackLayout
            {
                Padding = 20,
                Spacing = 15,
                Children =
                {
                    CreatePickerSection(Localization.GetString("SelectLanguage"), languagePicker),
                    CreatePickerSection(Localization.GetString("AppThemeTitle"), themePicker),
                    trumpStyleHeaderRow,
                    trumpPalettePicker,
                    CreatePickerSection(Localization.GetString("DefaultBidTotalRuleStartRound"), bidTotalRulePicker),
                    keepScreenAwakeRow,
                    boldAllTextRow
                }
            }
        };
    }

    // Builds a vertical section with a caption header above the picker. Using a real Label
    // (instead of Picker.Title) lets the caption follow the app-wide "bold all text" style.
    private static VerticalStackLayout CreatePickerSection(string caption, Picker picker)
    {
        // Clear the native title so the caption is not shown twice.
        picker.Title = string.Empty;

        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label
                {
                    Text = caption,
                    VerticalTextAlignment = TextAlignment.Center
                },
                picker
            }
        };
    }

    // Builds a settings row with a caption label and a trailing control.
    private static HorizontalStackLayout CreateSettingRow(string caption, View control, bool boldLabel = false)
    {
        var label = new Label
        {
            Text = caption,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Start
        };

        // Only force bold here; leaving the default lets the app-wide "bold all text" style apply.
        if (boldLabel)
        {
            label.FontAttributes = FontAttributes.Bold;
        }

        return new HorizontalStackLayout
        {
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                label,
                control
            }
        };
    }

    private static Border CreateIconBorder()
    {
        return new Border
        {
            WidthRequest = 28,
            HeightRequest = 28,
            Stroke = Colors.Black,
            StrokeThickness = 1,
            Padding = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 14 }
        };
    }

    private static View CreateColorIcon(Color color)
    {
        var border = CreateIconBorder();
        border.BackgroundColor = color;
        return border;
    }

    private static View CreateSuitIcon(string symbol, Color color)
    {
        var border = CreateIconBorder();
        border.BackgroundColor = Colors.White;
        border.Content = new Label
        {
            Text = symbol,
            TextColor = color,
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        return border;
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
}

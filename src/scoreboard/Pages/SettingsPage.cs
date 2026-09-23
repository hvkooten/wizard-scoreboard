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

    private readonly ITrumpPaletteService trumpPaletteService;
    private readonly Picker languagePicker;
    private readonly Picker trumpPalettePicker;
    private readonly HorizontalStackLayout trumpPalettePreviewLayout;
    private readonly Picker bidTotalRulePicker;

    public SettingsPage(ITrumpPaletteService trumpPaletteService)
    {
        PageTitleHelper.Apply(this, Localization.GetString("Settings"));

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

        // Default bid total rule start round applied when creating a new group.
        bidTotalRulePicker = new Picker { Title = Localization.GetString("DefaultBidTotalRuleStartRound") };
        bidTotalRulePicker.Items.Add(Localization.GetString("Disabled"));
        bidTotalRulePicker.Items.Add(Localization.GetString("Player Count"));
        bidTotalRulePicker.Items.Add(Localization.GetString("DoublePlayerCount"));
        for (var round = 1; round <= 13; round++)
        {
            bidTotalRulePicker.Items.Add(round.ToString());
        }
        bidTotalRulePicker.SelectedIndex = BidTotalRulePicker.IndexFromValue(AppSettings.DefaultBidTotalRuleStartRound);
        bidTotalRulePicker.SelectedIndexChanged += (s, e) =>
        {
            if (bidTotalRulePicker.SelectedIndex >= 0)
            {
                AppSettings.DefaultBidTotalRuleStartRound = BidTotalRulePicker.ValueFromIndex(bidTotalRulePicker.SelectedIndex);
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
                    languagePicker,
                    trumpStyleHeaderRow,
                    trumpPalettePicker,
                    bidTotalRulePicker
                }
            }
        };
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
}

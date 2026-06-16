using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using WizardScoreboard.Resources;

namespace WizardScoreboard.Pages;

public class RulesPage : ContentPage
{
    public RulesPage()
    {
        Title = $"{Localization.GetString("Rules")} · Wizard";

        var label = new Label
        {
            Text = Localization.GetString("RulesIntro"),
            Margin = new Thickness(10)
        };

        var webView = new WebView
        {
            Source = new UrlWebViewSource
            {
                Url = "https://cdn.1j1ju.com/medias/f1/8e/ad-wizard-rulebook.pdf"
            },
            HeightRequest = 500
        };

        var fallback = new Label
        {
            Text = Localization.GetString("RulesFallback"),
            TextColor = Colors.Gray,
            Margin = new Thickness(10)
        };

        var openRulesButton = new Button
        {
            Text = Localization.GetString("OpenRulesInBrowser")
        };
        openRulesButton.Clicked += async (s, e) =>
        {
            await Launcher.OpenAsync("https://cdn.1j1ju.com/medias/f1/8e/ad-wizard-rulebook.pdf");
        };

        Content = new ScrollView
        {
            Content = new StackLayout
            {
                Padding = 10,
                Children = { label, webView, fallback, openRulesButton }
            }
        };
    }
}

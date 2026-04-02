using Microsoft.Maui.Controls;
using WizardScoreboard.Resources;

namespace WizardScoreboard.Pages;

public class RulesPage : ContentPage
{
    public RulesPage()
    {
        Title = Localization.GetString("Rules");

        var label = new Label
        {
            Text = "Wizard game rules are available below.\nIf PDF cannot render, use the link.",
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
            Text = "If PDF does not load, please check the rules at:\nhttps://cdn.1j1ju.com/medias/f1/8e/ad-wizard-rulebook.pdf",
            TextColor = Colors.Gray,
            Margin = new Thickness(10)
        };

        Content = new ScrollView
        {
            Content = new StackLayout
            {
                Padding = 10,
                Children = { label, webView, fallback }
            }
        };
    }
}

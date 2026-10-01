using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

internal sealed class BetaWelcomePage : ContentPage
{
    internal BetaWelcomePage(Action close)
    {
        ArgumentNullException.ThrowIfNull(close);

        Title = Localization.GetString("BetaWelcomeTitle");
        SafeAreaEdges = SafeAreaEdges.All;

        var locationKey = DeviceInfo.Platform == DevicePlatform.WinUI
            ? "BetaReportLocationWindows"
            : "BetaReportLocationMobile";
        var heading = new HeaderLabel
        {
            Text = Title,
            FontFamily = "WizardFont",
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            TextColor = AppColors.TitleText
        };
        SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level1);

        var guidance = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 20,
                Children =
                {
                    heading,
                    new Label
                    {
                        Text = string.Format(Localization.GetString("BetaWelcomeBody"),
                            Localization.GetString("ReportBug"), Localization.GetString(locationKey)),
                        FontSize = 16
                    }
                }
            }
        };

        var closeButton = new Button { Text = Localization.GetString("BetaWelcomeClose") };
        closeButton.Clicked += (s, e) =>
        {
            closeButton.IsEnabled = false;
            try
            {
                close();
            }
            finally
            {
                closeButton.IsEnabled = true;
            }
        };

        var footer = new VerticalStackLayout
        {
            Spacing = 12,
            Children = { UiFactory.CreateBetaWelcomeOptOut(), closeButton }
        };
        var layout = new Grid
        {
            Padding = 24,
            RowSpacing = 20,
            RowDefinitions = { new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) }
        };
        layout.Add(guidance);
        layout.Add(footer, 0, 1);
        Content = layout;
        App.ApplyBoldToVisualTree(this, AppSettings.BoldAllText);
    }
}

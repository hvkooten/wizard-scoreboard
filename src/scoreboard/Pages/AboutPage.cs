using WizardScoreboard.Resources;

namespace WizardScoreboard.Pages;

public class AboutPage : ContentPage
{
    public AboutPage()
    {
        PageTitleHelper.Apply(this, Localization.GetString("About"));

        var icon = new Image
        {
            Source = "logo.png",
            WidthRequest = 240,
            HeightRequest = 96,
            HorizontalOptions = LayoutOptions.Center,
            Aspect = Aspect.AspectFit
        };

        var nameLabel = new Label
        {
            Text = AppInfo.Current.Name,
            FontFamily = "WizardFont",
            FontSize = 32,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#8A1C0A"),
            Shadow = new Shadow
            {
                Brush = Color.FromArgb("#FF7A18"),
                Offset = new Point(0, 0),
                Radius = 10,
                Opacity = 1f
            },
            HorizontalTextAlignment = TextAlignment.Center
        };

        var versionLabel = new Label
        {
            Text = string.Format(Localization.GetString("VersionTemplate"), $"{AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString})"),
            FontSize = 16,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var copyrightLabel = new Label
        {
            Text = Localization.GetString("Copyright"),
            FontSize = 14,
            TextColor = Colors.Gray,
            HorizontalTextAlignment = TextAlignment.Center
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 16,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Children = { icon, nameLabel, versionLabel, copyrightLabel }
            }
        };
    }
}

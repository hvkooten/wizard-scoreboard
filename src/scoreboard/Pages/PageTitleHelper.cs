using Microsoft.Maui.Controls;

namespace WizardScoreboard.Pages;

internal static class PageTitleHelper
{
    public static void Apply(ContentPage page, string leftTitle)
    {
        page.Title = leftTitle;

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center,
            MinimumWidthRequest = 260
        };

        var titleLabel = new Label
        {
            Text = leftTitle,
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Start,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var appLabel = new Label
        {
            Text = "Wizards",
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.End,
            HorizontalOptions = LayoutOptions.End
        };

        grid.Add(titleLabel, 0, 0);
        grid.Add(appLabel, 1, 0);

        void SyncTitleWidth()
        {
            if (page.Width > 0)
            {
                // Keep title view width in sync with window size so the right label stays anchored.
                grid.WidthRequest = Math.Max(260, page.Width - 48);
            }
        }

        page.SizeChanged += (_, _) => SyncTitleWidth();
        SyncTitleWidth();

        Shell.SetTitleView(page, grid);
    }
}
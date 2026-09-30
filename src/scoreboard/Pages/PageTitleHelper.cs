using WizardScoreboard.Resources;

namespace WizardScoreboard.Pages;

internal static class PageTitleHelper
{
    public static void Apply(ContentPage page, string leftTitle)
    {
        page.Title = leftTitle;

        var isAndroid = DeviceInfo.Platform == DevicePlatform.Android;
        Shell.SetTabBarIsVisible(page, !isAndroid);

        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto }
            },
            // On Android leave room on the left for the flyout (hamburger) icon and a small
            // gap on the right so the "Wizard" title is not clipped by the screen edge.
            Padding = isAndroid ? new Thickness(8, 0, 16, 0) : new Thickness(0),
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Center,
            MinimumWidthRequest = 260
        };

        var titleRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 12
        };

        var titleLabel = new HeaderLabel
        {
            Text = leftTitle,
            FontFamily = "WizardFont",
            FontSize = 26,
            CharacterSpacing = 1,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#8A1C0A"),
            Shadow = new Shadow
            {
                Brush = Color.FromArgb("#FF7A18"),
                Offset = new Point(0, 0),
                Radius = 10,
                Opacity = 1f
            },
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Start,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        var appLabel = new HeaderLabel
        {
            Text = "Wizard",
            FontFamily = "WizardFont",
            // Evoke the fiery "Wizard" box-art logo: deep lava-red glyphs with a glowing amber halo.
            FontSize = 28,
            CharacterSpacing = 2,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#8A1C0A"),
            Shadow = new Shadow
            {
                Brush = Color.FromArgb("#FF7A18"),
                Offset = new Point(0, 0),
                Radius = 10,
                Opacity = 1f
            },
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.End,
            HorizontalOptions = LayoutOptions.End
        };

        titleRow.Add(titleLabel, 0, 0);
        titleRow.Add(appLabel, 1, 0);
        grid.Add(titleRow, 0, 0);

        // The header stays visually stable regardless of the "bold all text" setting. The labels are
        // HeaderLabel instances (never targeted by the global implicit Label style) and deliberately
        // avoid FontAttributes.Bold: the MedievalSharp font has no real bold weight, so faux-bold would
        // be re-rasterized inconsistently on re-layout. Distinction comes from size, color and glow.
        void SyncTitleWidth()
        {
            if (page.Width > 0)
            {
                // Keep title view width in sync with window size so the right label stays anchored
                // and visible. Android reserves extra space on the left for the flyout icon.
                grid.WidthRequest = Math.Max(240, page.Width - (isAndroid ? 88 : 48));
            }
        }

        page.SizeChanged += (_, _) => SyncTitleWidth();
        SyncTitleWidth();

        Shell.SetTitleView(page, grid);
    }
}

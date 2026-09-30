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
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 1,
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
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 2,
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

        // The header must always stay bold, independent of the "bold all text" setting. The header
        // labels use the HeaderLabel subclass, which the global implicit Style(typeof(Label)) never
        // matches (implicit styles are exact-type only), so their bold styling is never overridden.
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

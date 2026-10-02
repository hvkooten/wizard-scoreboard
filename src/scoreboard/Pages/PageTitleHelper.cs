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
            // On Android the toolbar inset after the flyout (hamburger) icon is removed in
            // AppShell.Android, so a small left gap is enough. A gap on the right keeps the
            // "Wizard" title from being clipped by the screen edge.
            Padding = isAndroid ? new Thickness(4, 0, 16, 0) : new Thickness(0),
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
            TextColor = AppColors.TitleText,
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
            TextColor = AppColors.TitleText,
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

        // The header stays bold regardless of the "bold all text" setting. The labels are HeaderLabel
        // instances, which the global bold walk skips; on Android and iOS bold is enforced by HeaderLabelHandler.
        void SyncTitleWidth()
        {
            if (page.Width > 0)
            {
                // Keep title view width in sync with window size so the right label stays anchored
                // and visible. Android reserves extra space on the left for the flyout icon.
                grid.WidthRequest = Math.Max(240, page.Width - (isAndroid ? 56 : 48) - GetHorizontalSystemInsets());
            }
        }

        page.SizeChanged += (_, _) => SyncTitleWidth();
        SyncTitleWidth();

        Shell.SetTitleView(page, grid);
    }

    // In landscape the navigation bar, display cutout or notch sit at the sides. The page can extend under
    // them while the toolbar does not, so the title would be pushed off-screen without this correction.
    private static double GetHorizontalSystemInsets()
    {
#if ANDROID
        var decorView = Platform.CurrentActivity?.Window?.DecorView;
        if (decorView == null)
        {
            return 0;
        }

        var windowInsets = AndroidX.Core.View.ViewCompat.GetRootWindowInsets(decorView);
        if (windowInsets == null)
        {
            return 0;
        }

        var insets = windowInsets.GetInsets(
            AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars()
            | AndroidX.Core.View.WindowInsetsCompat.Type.DisplayCutout());
        var density = DeviceDisplay.Current.MainDisplayInfo.Density;
        return density > 0 ? (insets.Left + insets.Right) / density : 0;
#elif IOS || MACCATALYST
        var window = UIKit.UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIKit.UIWindowScene>()
            .SelectMany(scene => scene.Windows)
            .FirstOrDefault(w => w.IsKeyWindow);
        if (window == null)
        {
            return 0;
        }

        var safeArea = window.SafeAreaInsets;
        return safeArea.Left + safeArea.Right;
#else
        return 0;
#endif
    }
}

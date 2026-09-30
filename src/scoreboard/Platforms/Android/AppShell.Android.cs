using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomNavigation;

namespace WizardScoreboard;

public partial class AppShell
{
    // Keeps the bottom navigation labels compact so descenders are not clipped on Android.
    private const float TabLabelTextSizeSp = 11f;

    // Google's LabelVisibilityMode value that always shows the label text.
    private const int LabelVisibilityLabeled = 1;

    // Android renders the Shell tab bar via a BottomNavigationView whose labels are plain
    // TextViews that ignore MAUI styles, so set their typeface directly.
    partial void ApplyTabBarTextStylePlatform(bool bold)
    {
        if (Handler?.PlatformView is not Android.Views.View root)
        {
            return;
        }

        // Defer until the native view tree is laid out so the tab items exist.
        root.Post(() =>
        {
            var typeface = bold ? Typeface.DefaultBold : Typeface.Default;
            ApplyTypefaceToTextViews(root, typeface);
            ConfigureBottomNavigation(root);
        });
    }

    // Forces the bottom navigation to always show labels and shrinks their text so the
    // bottom of the letters is not cut off on Android.
    private static void ConfigureBottomNavigation(Android.Views.View view)
    {
        if (view is BottomNavigationView bottomNav)
        {
            bottomNav.LabelVisibilityMode = LabelVisibilityLabeled;
            ApplyLabelTextSize(bottomNav);
            return;
        }

        if (view is ViewGroup group)
        {
            for (var i = 0; i < group.ChildCount; i++)
            {
                var child = group.GetChildAt(i);
                if (child is not null)
                {
                    ConfigureBottomNavigation(child);
                }
            }
        }
    }

    private static void ApplyLabelTextSize(Android.Views.View view)
    {
        if (view is TextView textView)
        {
            textView.SetTextSize(ComplexUnitType.Sp, TabLabelTextSizeSp);
        }

        if (view is ViewGroup group)
        {
            for (var i = 0; i < group.ChildCount; i++)
            {
                var child = group.GetChildAt(i);
                if (child is not null)
                {
                    ApplyLabelTextSize(child);
                }
            }
        }
    }

    private static void ApplyTypefaceToTextViews(Android.Views.View view, Typeface? typeface)
    {
        if (view is TextView textView)
        {
            textView.SetTypeface(typeface, TypefaceStyle.Normal);
        }

        if (view is ViewGroup group)
        {
            for (var i = 0; i < group.ChildCount; i++)
            {
                var child = group.GetChildAt(i);
                if (child is not null)
                {
                    ApplyTypefaceToTextViews(child, typeface);
                }
            }
        }
    }
}

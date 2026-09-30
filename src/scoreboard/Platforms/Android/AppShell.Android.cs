using Android.Graphics;
using Android.Views;
using Android.Widget;

namespace WizardScoreboard;

public partial class AppShell
{
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
        });
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

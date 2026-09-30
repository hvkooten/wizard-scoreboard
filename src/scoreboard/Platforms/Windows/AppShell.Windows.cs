using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace WizardScoreboard;

public partial class AppShell
{
    // Windows renders the Shell tab bar with a NavigationView; its item labels are TextBlocks
    // that ignore MAUI styles, so walk the visual tree and set their FontWeight directly. Only the
    // NavigationViewItem tab labels are targeted, so the Shell TitleView header is never touched.
    partial void ApplyTabBarTextStylePlatform(bool bold)
    {
        if (Handler?.PlatformView is not FrameworkElement root)
        {
            return;
        }

        Windows.UI.Text.FontWeight weight = bold ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal;

        void Apply() => ApplyFontWeightToTextBlocks(root, weight);

        // Apply now, on Loaded, and via the dispatcher so late-created tab items are covered.
        Apply();
        root.Loaded += (s, e) => Apply();
        root.DispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, Apply);
    }

    private static void ApplyFontWeightToTextBlocks(DependencyObject parent, Windows.UI.Text.FontWeight weight)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is NavigationViewItem navItem)
            {
                // Only style the tab items' own TextBlocks. Do NOT recurse past the item into the
                // hosted page content, and never touch the Shell TitleView (the "Wizard"/page-title
                // header), whose TextBlocks live elsewhere in the tree and must stay unaffected.
                ApplyFontWeightWithinTabItem(navItem, weight);
                continue;
            }

            ApplyFontWeightToTextBlocks(child, weight);
        }
    }

    private static void ApplyFontWeightWithinTabItem(DependencyObject parent, Windows.UI.Text.FontWeight weight)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is TextBlock textBlock)
            {
                textBlock.FontWeight = weight;
            }

            ApplyFontWeightWithinTabItem(child, weight);
        }
    }
}

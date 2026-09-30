using WizardScoreboard.Resources;

namespace WizardScoreboard.Pages;

// Small factory/helpers for UI patterns repeated across multiple pages.
internal static class UiFactory
{
    // Applies the shared active/inactive styling to a toggle-style button.
    public static void ApplyToggleStyle(Button button, bool isActive)
    {
        button.BackgroundColor = isActive ? AppColors.Primary : AppColors.ToggleInactiveBg;
        button.TextColor = isActive ? Colors.White : AppColors.ToggleInactiveText;
    }

    // Standard red trash/delete button; size and font differ per usage.
    public static Button CreateDeleteButton(double width, double height, double? fontSize = null)
    {
        var button = new Button
        {
            Text = "\U0001F5D1",
            WidthRequest = width,
            HeightRequest = height,
            Padding = new Thickness(0),
            CornerRadius = 8,
            BackgroundColor = AppColors.DangerBg,
            TextColor = AppColors.DangerText
        };
        if (fontSize is { } size)
        {
            button.FontSize = size;
        }
        return button;
    }

    // Square icon button used for reorder (up/down) actions with shared toggle-inactive styling.
    public static Button CreateReorderButton(string glyph, bool isEnabled, EventHandler onClicked)
    {
        var button = new Button
        {
            Text = glyph,
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = new Thickness(0),
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            BackgroundColor = AppColors.ToggleInactiveBg,
            TextColor = AppColors.ToggleInactiveText,
            IsEnabled = isEnabled
        };
        button.Clicked += onClicked;
        return button;
    }

    // Small square +/- stepper button; the primary variant is filled, otherwise a subtle toggle style.
    public static Button CreateStepperButton(string glyph, bool isPrimary)
    {
        return new Button
        {
            Text = glyph,
            WidthRequest = 36,
            HeightRequest = 36,
            Padding = new Thickness(0),
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = isPrimary ? AppColors.Primary : AppColors.ToggleInactiveBg,
            TextColor = isPrimary ? Colors.White : AppColors.ToggleInactiveText
        };
    }

    // Shows a localized alert; centralizes the repeated title/message/Ok pattern.
    public static Task ShowMessageAsync(this Page page, string titleKey, string message, string buttonKey = "Ok") =>
        page.DisplayAlertAsync(
            Localization.GetString(titleKey),
            message,
            Localization.GetString(buttonKey));
}

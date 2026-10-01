using System.Diagnostics;
using System.Globalization;
using WizardScoreboard.Pages;
using WizardScoreboard.Resources;

namespace WizardScoreboard.Services;

internal static class BugReportService
{
    private const string Recipient = "nodorumsolutio@gmail.com";

    internal static async Task ComposeAsync(Page page)
    {
        ArgumentNullException.ThrowIfNull(page);

        var emailAction = Localization.GetString("BugReportOpenEmail");
        var copyAction = Localization.GetString("BugReportCopy");
        var action = await page.DisplayActionSheetAsync(Localization.GetString("ReportBug"),
            Localization.GetString("Cancel"), null, emailAction, copyAction);
        if (action != emailAction && action != copyAction)
        {
            return;
        }

        var app = AppInfo.Current;
        var device = DeviceInfo.Current;
        var display = DeviceDisplay.Current.MainDisplayInfo;
        var logicalSize = display.Density > 0
            ? string.Create(CultureInfo.InvariantCulture,
                $"{display.Width / display.Density:0.##} x {display.Height / display.Density:0.##}")
            : "? x ?";
        var body = string.Format(CultureInfo.InvariantCulture,
            Localization.GetString("BugReportBodyTemplate"),
            app.VersionString, app.BuildString, device.Platform, device.VersionString,
            device.Manufacturer, device.Model, device.Idiom, device.DeviceType,
            string.Create(CultureInfo.InvariantCulture, $"{display.Width:0} x {display.Height:0}"), logicalSize,
            display.Density, display.Orientation, display.RefreshRate,
            CultureInfo.CurrentCulture.Name).ReplaceLineEndings("\r\n");
        var subject = string.Format(CultureInfo.InvariantCulture,
            Localization.GetString("BugReportSubject"), app.VersionString);

        if (action == copyAction)
        {
            await CopyReportAsync(page, subject, body);
            return;
        }

        // mailto delegates to the user's default email handler instead of an in-app composer.
        var uri = new Uri($"mailto:{Recipient}?subject="
            + Uri.EscapeDataString(subject)
            + "&body=" + Uri.EscapeDataString(body));
        var opened = false;
        try
        {
            opened = await Launcher.Default.OpenAsync(uri);
        }
        catch (FeatureNotSupportedException ex)
        {
            Trace.TraceError("{0}", ex);
        }
        catch (InvalidOperationException ex)
        {
            Trace.TraceError("{0}", ex);
        }
#if WINDOWS
        catch (System.ComponentModel.Win32Exception ex)
        {
            Trace.TraceError("{0}", ex);
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            Trace.TraceError("{0}", ex);
        }
#elif ANDROID
        catch (Android.Content.ActivityNotFoundException ex)
        {
            Trace.TraceError("{0}", ex);
        }
#endif

        if (!opened)
        {
            await page.DisplayAlertAsync(Localization.GetString("ErrorTitle"),
                Localization.GetString("BugReportEmailUnavailable"), Localization.GetString("Ok"));
        }
    }

    private static async Task CopyReportAsync(Page page, string subject, string body)
    {
        var text = string.Format(CultureInfo.CurrentCulture,
            Localization.GetString("BugReportClipboardTemplate"), Recipient, subject, body)
            .ReplaceLineEndings("\r\n");
        var copied = false;
        try
        {
            await Clipboard.Default.SetTextAsync(text);
            copied = true;
        }
        catch (FeatureNotSupportedException ex)
        {
            Trace.TraceError("{0}", ex);
        }
        catch (InvalidOperationException ex)
        {
            Trace.TraceError("{0}", ex);
        }
#if WINDOWS
        catch (System.Runtime.InteropServices.COMException ex)
        {
            Trace.TraceError("{0}", ex);
        }
#endif

        await page.ShowMessageAsync(copied ? "InfoTitle" : "ErrorTitle",
            Localization.GetString(copied ? "BugReportCopied" : "BugReportCopyFailed"));
    }
}

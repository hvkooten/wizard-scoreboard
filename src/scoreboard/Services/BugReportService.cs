using System.Diagnostics;
using System.Globalization;
using WizardScoreboard.Resources;

namespace WizardScoreboard.Services;

internal static class BugReportService
{
    internal static async Task ComposeAsync(Page page)
    {
        ArgumentNullException.ThrowIfNull(page);

        var app = AppInfo.Current;
        var device = DeviceInfo.Current;
        var display = DeviceDisplay.Current.MainDisplayInfo;
        var logicalSize = display.Density > 0
            ? $"{display.Width / display.Density:0.##} x {display.Height / display.Density:0.##}"
            : "? x ?";
        var body = string.Format(CultureInfo.CurrentCulture,
            Localization.GetString("BugReportBodyTemplate"),
            app.VersionString, app.BuildString, device.Platform, device.VersionString,
            device.Manufacturer, device.Model, device.Idiom, device.DeviceType,
            $"{display.Width:0} x {display.Height:0}", logicalSize,
            display.Density, display.Orientation, display.RefreshRate,
            CultureInfo.CurrentCulture.Name);

        // mailto delegates to the user's default email handler instead of an in-app composer.
        var uri = new Uri("mailto:nodorumsolutio@gmail.com?subject="
            + Uri.EscapeDataString(Localization.GetString("BugReportSubject"))
            + "&body=" + Uri.EscapeDataString(body.ReplaceLineEndings("\r\n")));
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
}

using System.Text;
using Foundation;
using ObjCRuntime;

namespace WizardScoreboard;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            LogFatal("AppDomain.UnhandledException", e.ExceptionObject as Exception, e.ExceptionObject);

        TaskScheduler.UnobservedTaskException += (sender, e) =>
            LogFatal("TaskScheduler.UnobservedTaskException", e.Exception);

        Runtime.MarshalManagedException += (sender, args) =>
            LogFatal("Runtime.MarshalManagedException", args.Exception);

        return MauiProgram.CreateMauiApp();
    }

    // Mirrors the Android diagnostics: writes a fully-expanded report to the console and to a
    // persistent crash-log file in the app's Documents/crashlogs folder.
    private static void LogFatal(string source, Exception? exception, object? rawObject = null)
    {
        string report;
        try
        {
            report = BuildReport(source, exception, rawObject);
        }
        catch (Exception formatting)
        {
            report = $"[{source}] Failed to format exception: {formatting}\nOriginal: {exception}";
        }

        Console.Error.WriteLine(report);
        TryPersist(report);
    }

    private static string BuildReport(string source, Exception? exception, object? rawObject)
    {
        var sb = new StringBuilder();
        sb.AppendLine("===== WizardScoreboard crash report =====");
        sb.Append("Source : ").AppendLine(source);
        sb.Append("Time   : ").AppendLine(DateTimeOffset.Now.ToString("O"));
        sb.Append("Thread : ").AppendLine(Environment.CurrentManagedThreadId.ToString());

        if (exception is null)
        {
            sb.Append("Raw    : ").AppendLine(rawObject?.ToString() ?? "<null>");
            return sb.ToString();
        }

        var depth = 0;
        for (var ex = exception; ex is not null; ex = ex.InnerException, depth++)
        {
            sb.AppendLine();
            sb.Append("--- Exception[").Append(depth).Append("]: ").Append(ex.GetType().FullName).AppendLine(" ---");
            sb.Append("Message: ").AppendLine(ex.Message);
            sb.AppendLine("Managed stack:");
            sb.AppendLine(ex.StackTrace ?? "<none>");
        }

        return sb.ToString();
    }

    private static void TryPersist(string report)
    {
        try
        {
            var folder = WizardScoreboard.Services.CrashLogStore.GetFolder();
            if (folder is null)
            {
                return;
            }

            var file = WizardScoreboard.Services.CrashLogStore.Write(folder, report);
            Console.Error.WriteLine($"Crash report written to: {file}");
        }
        catch (IOException persistError)
        {
            Console.Error.WriteLine($"Failed to persist crash report: {persistError}");
        }
        catch (UnauthorizedAccessException persistError)
        {
            Console.Error.WriteLine($"Failed to persist crash report: {persistError}");
        }
    }
}

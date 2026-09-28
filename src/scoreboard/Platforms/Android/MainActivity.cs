using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Microsoft.Maui;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WizardScoreboard;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    const string TAG = "AppDiagnostics";

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Ensure platform is initialized so CurrentActivity can be detected by libraries
        Microsoft.Maui.ApplicationModel.Platform.Init(this, savedInstanceState);

        // Global handlers to capture unhandled exceptions for diagnostics
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            LogFatal("AppDomain.UnhandledException", e.ExceptionObject as Exception, e.ExceptionObject);
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            LogFatal("TaskScheduler.UnobservedTaskException", e.Exception);
            // e.SetObserved(); // optionally mark observed
        };

        AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
        {
            LogFatal("AndroidEnvironment.UnhandledExceptionRaiser", args.Exception);
            // args.Handled = false; // set true to swallow after logging
        };
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        Microsoft.Maui.ApplicationModel.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }

    // Helper to wrap callbacks invoked from Java/Android so exceptions are logged with full details
    public static void SafeInvoke(Action action, string tag = TAG)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            LogFatal("SafeInvoke", ex);
            throw; // rethrow so runtime behavior is unchanged; remove if you want to swallow
        }
    }

    public static async Task SafeInvokeAsync(Func<Task> func, string tag = TAG)
    {
        try
        {
            await func().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogFatal("SafeInvokeAsync", ex);
            throw;
        }
    }

    // Central diagnostic sink: writes a fully-expanded report to logcat AND to a
    // persistent crash-log file so AI2 can pull it via adb even without a live logcat capture.
    //   adb pull /sdcard/Android/data/<package>/files/crashlogs ./crashlogs
    //   (or) adb exec-out run-as <package> cat files/crashlogs/<file>.log
    static void LogFatal(string source, Exception? exception, object? rawObject = null)
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

        // 1) Always emit to logcat (chunked to avoid Android's per-line truncation).
        foreach (var chunk in ChunkForLogcat(report))
        {
            Log.Error(TAG, chunk);
        }

        // 2) Best-effort persist to a file for offline retrieval.
        TryPersist(report);
    }

    static string BuildReport(string source, Exception? exception, object? rawObject)
    {
        var sb = new StringBuilder();
        sb.AppendLine("===== WizardScoreboard crash report =====");
        sb.Append("Source : ").AppendLine(source);
        sb.Append("Time   : ").AppendLine(DateTimeOffset.Now.ToString("O"));
        sb.Append("Thread : ").AppendLine(System.Environment.CurrentManagedThreadId.ToString());

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

            // Surface the underlying Java throwable when the managed exception crossed JNI.
            AppendJavaDetails(sb, ex);

            sb.AppendLine("Managed stack:");
            sb.AppendLine(ex.StackTrace ?? "<none>");
        }

        return sb.ToString();
    }

    // JavaProxyThrowable (and other Java.Lang.Throwable-backed exceptions) hide the original
    // Java exception type + Java stack trace, which is exactly what the diagnosis needs.
    static void AppendJavaDetails(StringBuilder sb, Exception ex)
    {
        Java.Lang.Throwable? throwable = ex switch
        {
            Java.Lang.Throwable t => t,
            _ when ex.GetType().FullName == "Android.Runtime.JavaProxyThrowable" => TryGetInnerThrowable(ex),
            _ => null
        };

        if (throwable is null)
        {
            return;
        }

        sb.Append("Java type   : ").AppendLine(throwable.Class?.Name ?? throwable.GetType().FullName);
        sb.Append("Java message: ").AppendLine(throwable.Message ?? "<none>");
        sb.AppendLine("Java stack:");
        foreach (var frame in throwable.GetStackTrace() ?? Array.Empty<Java.Lang.StackTraceElement>())
        {
            sb.Append("    at ").AppendLine(frame?.ToString());
        }

        if (throwable.Cause is { } cause)
        {
            sb.Append("Java cause  : ").AppendLine(cause.ToString());
        }
    }

    static Java.Lang.Throwable? TryGetInnerThrowable(Exception ex)
    {
        // JavaProxyThrowable exposes the wrapped Java throwable via a property in some runtimes;
        // fall back to reflection so we work across Xamarin/Mono/.NET Android versions.
        var prop = ex.GetType().GetProperty("JavaException") ?? ex.GetType().GetProperty("InnerJavaException");
        return prop?.GetValue(ex) as Java.Lang.Throwable;
    }

    static void TryPersist(string report)
    {
        try
        {
            var dir = Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath
                      ?? Android.App.Application.Context.FilesDir?.AbsolutePath;
            if (string.IsNullOrEmpty(dir))
            {
                return;
            }

            var folder = Path.Combine(dir, "crashlogs");
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, $"crash-{DateTime.Now:yyyyMMdd-HHmmss-fff}.log");
            File.WriteAllText(file, report);
            Log.Error(TAG, $"Crash report written to: {file}");
        }
        catch (Exception persistError)
        {
            Log.Error(TAG, $"Failed to persist crash report: {persistError}");
        }
    }

    // Android truncates very long log lines (~4000 chars); split so nothing is lost.
    static System.Collections.Generic.IEnumerable<string> ChunkForLogcat(string text)
    {
        const int max = 3500;
        for (var i = 0; i < text.Length; i += max)
        {
            yield return text.Substring(i, Math.Min(max, text.Length - i));
        }
    }
}


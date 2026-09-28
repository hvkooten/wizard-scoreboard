using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Microsoft.Maui;
using System;
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
            var ex = e.ExceptionObject as Exception;
            Log.Error(TAG, $"AppDomain Unhandled: {ex?.ToString() ?? e.ExceptionObject?.ToString()}");
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Log.Error(TAG, $"UnobservedTask: {e.Exception?.ToString() ?? e.Exception?.InnerException?.ToString()}");
            // e.SetObserved(); // optionally mark observed
        };

        AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
        {
            Log.Error(TAG, $"AndroidEnvironment: {args.Exception?.ToString()}");
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
            Log.Error(tag, ex.ToString());
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
            Log.Error(tag, ex.ToString());
            throw;
        }
    }
}


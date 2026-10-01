
namespace WizardScoreboard.Services;

// Controls whether the device screen is kept on. Honors the KeepScreenAwakeDuringGame setting
// and always releases the wake lock when the app is not in the foreground.
public class ScreenWakeService : IScreenWakeService
{
    private readonly IDeviceDisplay? deviceDisplay;
    private readonly UserSettings? settings;
    private bool gameInProgress;

    /// <summary>Creates the screen-wake service using the current device and app settings.</summary>
    public ScreenWakeService()
    {
    }

    /// <summary>Creates the screen-wake service using the supplied device and settings.</summary>
    public ScreenWakeService(IDeviceDisplay deviceDisplay, UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(deviceDisplay);
        ArgumentNullException.ThrowIfNull(settings);
        this.deviceDisplay = deviceDisplay;
        this.settings = settings;
    }

    // Requests the screen to stay on while a game is in progress.
    public void RequestKeepAwake()
    {
        gameInProgress = true;
        Apply();
    }

    // Releases the request to keep the screen on (e.g. when a game ends).
    public void ReleaseKeepAwake()
    {
        gameInProgress = false;
        Apply();
    }

    // Re-evaluates the wake state, taking the current setting into account.
    // Called when the app resumes so a changed setting takes effect.
    public void Refresh() => Apply();

    // Forces the screen wake lock off, e.g. when the app moves to the background.
    public void Suspend() => SetKeepScreenOn(false);

    private void Apply() => SetKeepScreenOn(gameInProgress
        && (settings?.KeepScreenAwakeDuringGame ?? AppSettings.KeepScreenAwakeDuringGame));

    private void SetKeepScreenOn(bool keepOn)
    {
        // DeviceDisplay needs the current Activity on Android. During early startup the page
        // can be constructed (via DI) before the Activity is ready, in which case accessing
        // KeepScreenOn throws "The current Activity cannot be detected". Keeping the screen on
        // is best-effort UX, so any timing issue is swallowed instead of crashing the app.
        try
        {
            var display = deviceDisplay ?? DeviceDisplay.Current;
            if (display.KeepScreenOn != keepOn)
            {
                display.KeepScreenOn = keepOn;
            }
        }
        catch (NullReferenceException)
        {
            // Current Activity not available yet; ignore and let a later Apply/Refresh retry.
        }
        catch (InvalidOperationException)
        {
            // Platform display not ready; ignore.
        }
    }
}

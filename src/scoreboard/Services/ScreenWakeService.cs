
namespace WizardScoreboard.Services;

// Controls whether the device screen is kept on. Honors the KeepScreenAwakeDuringGame setting
// and always releases the wake lock when the app is not in the foreground.
public class ScreenWakeService : IScreenWakeService
{
    private bool gameInProgress;

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

    private void Apply() => SetKeepScreenOn(gameInProgress && AppSettings.KeepScreenAwakeDuringGame);

    private static void SetKeepScreenOn(bool keepOn)
    {
        if (DeviceDisplay.Current.KeepScreenOn != keepOn)
        {
            DeviceDisplay.Current.KeepScreenOn = keepOn;
        }
    }
}

namespace WizardScoreboard.Services;

// Manages keeping the device screen on while a game is in progress.
public interface IScreenWakeService
{
    // Requests the screen to stay on while a game is in progress.
    void RequestKeepAwake();

    // Releases the request to keep the screen on.
    void ReleaseKeepAwake();

    // Re-evaluates the wake state so a changed setting takes effect.
    void Refresh();

    // Forces the screen wake lock off, e.g. when the app is backgrounded.
    void Suspend();
}

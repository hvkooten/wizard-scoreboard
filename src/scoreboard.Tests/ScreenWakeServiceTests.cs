using System;
using NUnit.Framework;
using WizardScoreboard.Services;

namespace WizardScoreboard.Tests;

public class ScreenWakeServiceTests
{
    [Test]
    public void Constructor_NullDisplayThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ScreenWakeService(null!, new UserSettings(new MemoryPreferences())));
    }

    [Test]
    public void Constructor_NullSettingsThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new ScreenWakeService(new TestDeviceDisplay(), null!));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void RequestKeepAwake_HonorsUserPreference(bool enabled)
    {
        var display = new TestDeviceDisplay();
        var settings = new UserSettings(new MemoryPreferences()) { KeepScreenAwakeDuringGame = enabled };
        var service = new ScreenWakeService(display, settings);

        service.RequestKeepAwake();

        Assert.That(display.KeepScreenOn, Is.EqualTo(enabled));
    }

    [Test]
    public void ReleaseKeepAwake_TurnsDisplayWakeOff()
    {
        var display = new TestDeviceDisplay();
        var settings = new UserSettings(new MemoryPreferences()) { KeepScreenAwakeDuringGame = true };
        var service = new ScreenWakeService(display, settings);
        service.RequestKeepAwake();

        service.ReleaseKeepAwake();

        Assert.That(display.KeepScreenOn, Is.False);
    }

    [Test]
    public void Suspend_TurnsDisplayWakeOff()
    {
        var display = new TestDeviceDisplay();
        var settings = new UserSettings(new MemoryPreferences()) { KeepScreenAwakeDuringGame = true };
        var service = new ScreenWakeService(display, settings);
        service.RequestKeepAwake();

        service.Suspend();

        Assert.That(display.KeepScreenOn, Is.False);
    }

    [Test]
    public void Refresh_AfterSuspendRestoresWakeForActiveGame()
    {
        var display = new TestDeviceDisplay();
        var settings = new UserSettings(new MemoryPreferences()) { KeepScreenAwakeDuringGame = true };
        var service = new ScreenWakeService(display, settings);
        service.RequestKeepAwake();
        service.Suspend();

        service.Refresh();

        Assert.That(display.KeepScreenOn, Is.True);
    }

    [Test]
    public void Refresh_AfterReleaseDoesNotRestoreWake()
    {
        var display = new TestDeviceDisplay();
        var settings = new UserSettings(new MemoryPreferences()) { KeepScreenAwakeDuringGame = true };
        var service = new ScreenWakeService(display, settings);
        service.RequestKeepAwake();
        service.ReleaseKeepAwake();

        service.Refresh();

        Assert.That(display.KeepScreenOn, Is.False);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Refresh_ReevaluatesChangedPreference(bool enabled)
    {
        var display = new TestDeviceDisplay();
        var settings = new UserSettings(new MemoryPreferences()) { KeepScreenAwakeDuringGame = !enabled };
        var service = new ScreenWakeService(display, settings);
        service.RequestKeepAwake();
        settings.KeepScreenAwakeDuringGame = enabled;

        service.Refresh();

        Assert.That(display.KeepScreenOn, Is.EqualTo(enabled));
    }

    [Test]
    public void Refresh_WithoutGameDoesNotKeepDisplayAwake()
    {
        var display = new TestDeviceDisplay();
        var settings = new UserSettings(new MemoryPreferences()) { KeepScreenAwakeDuringGame = true };
        var service = new ScreenWakeService(display, settings);

        service.Refresh();

        Assert.That(display.KeepScreenOn, Is.False);
    }

    [Test]
    public void RepeatedRequests_DoNotWriteUnchangedDisplayState()
    {
        var display = new TestDeviceDisplay();
        var settings = new UserSettings(new MemoryPreferences()) { KeepScreenAwakeDuringGame = true };
        var service = new ScreenWakeService(display, settings);
        service.RequestKeepAwake();

        service.RequestKeepAwake();
        service.Refresh();

        Assert.That(display.SetCount, Is.EqualTo(1));
    }

    [Test]
    public void RequestKeepAwake_UnavailableDisplayDoesNotThrow()
    {
        var display = new TestDeviceDisplay { IsAvailable = false };
        var settings = new UserSettings(new MemoryPreferences()) { KeepScreenAwakeDuringGame = true };
        var service = new ScreenWakeService(display, settings);

        Assert.DoesNotThrow(() => service.RequestKeepAwake());
    }

    [Test]
    public void Refresh_WhenDisplayBecomesAvailableAppliesPendingRequest()
    {
        var display = new TestDeviceDisplay { IsAvailable = false };
        var settings = new UserSettings(new MemoryPreferences()) { KeepScreenAwakeDuringGame = true };
        var service = new ScreenWakeService(display, settings);
        service.RequestKeepAwake();
        display.IsAvailable = true;

        service.Refresh();

        Assert.That(display.KeepScreenOn, Is.True);
    }
}

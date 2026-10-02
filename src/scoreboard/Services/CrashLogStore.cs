namespace WizardScoreboard.Services;

/// <summary>
/// Stores crash reports written by the platform crash handlers and hands out reports that have
/// not yet been offered to the user, so they can be added to a bug report on the next start.
/// </summary>
public static class CrashLogStore
{
    private const string PendingExtension = ".log";
    private const string HandledExtension = ".handled";

    // mailto URIs are truncated by some email apps; keep the crash log well below common limits.
    private const int DefaultMaxLength = 1500;

    /// <summary>
    /// Gets the platform folder that holds crash logs, or <c>null</c> when crash logging is not supported.
    /// </summary>
    public static string? GetFolder()
    {
#if ANDROID
        var root = Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath
                   ?? Android.App.Application.Context.FilesDir?.AbsolutePath;
        return string.IsNullOrEmpty(root) ? null : Path.Combine(root, "crashlogs");
#elif IOS
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "crashlogs");
#else
        return null;
#endif
    }

    /// <summary>
    /// Writes a crash report to <paramref name="folder"/> and returns the created file path.
    /// </summary>
    public static string Write(string folder, string report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(report);

        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, $"crash-{DateTime.Now:yyyyMMdd-HHmmss-fff}{PendingExtension}");
        File.WriteAllText(file, report);
        return file;
    }

    /// <summary>
    /// Returns the most recent pending crash report (truncated to <paramref name="maxLength"/>) and marks
    /// all pending reports as handled so they are offered only once. Returns <c>null</c> when none are pending.
    /// Handled files are kept for retrieval via adb or the file system.
    /// </summary>
    public static string? TakePendingReport(string folder, int maxLength = DefaultMaxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLength);

        if (!Directory.Exists(folder))
        {
            return null;
        }

        var pending = Directory.GetFiles(folder, $"*{PendingExtension}")
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
        if (pending.Count == 0)
        {
            return null;
        }

        var report = File.ReadAllText(pending[^1]);
        foreach (var file in pending)
        {
            File.Move(file, Path.ChangeExtension(file, HandledExtension), overwrite: true);
        }

        return report.Length <= maxLength ? report : report[..maxLength] + "\n...";
    }
}

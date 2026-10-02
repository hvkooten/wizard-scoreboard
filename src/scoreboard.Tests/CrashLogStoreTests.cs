using System;
using System.IO;
using NUnit.Framework;
using WizardScoreboard.Services;

namespace WizardScoreboard.Tests;

public class CrashLogStoreTests
{
    private static string CreateFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "WizardScoreboardTests", Guid.NewGuid().ToString("N"));
        TestContext.WriteLine($"Crash log folder: {folder}");
        return folder;
    }

    [Test]
    public void TakePendingReport_WhenFolderDoesNotExist_ReturnsNull()
    {
        var folder = CreateFolder();

        Assert.That(CrashLogStore.TakePendingReport(folder), Is.Null);
    }

    [Test]
    public void TakePendingReport_WhenReportWritten_ReturnsReport()
    {
        var folder = CreateFolder();
        CrashLogStore.Write(folder, "boom");

        Assert.That(CrashLogStore.TakePendingReport(folder), Is.EqualTo("boom"));
    }

    [Test]
    public void TakePendingReport_WhenCalledTwice_ReturnsNullSecondTime()
    {
        var folder = CreateFolder();
        CrashLogStore.Write(folder, "boom");
        CrashLogStore.TakePendingReport(folder);

        Assert.That(CrashLogStore.TakePendingReport(folder), Is.Null);
    }

    [Test]
    public void TakePendingReport_WhenMultipleReports_ReturnsMostRecent()
    {
        var folder = CreateFolder();
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "crash-20240101-000000-000.log"), "old");
        File.WriteAllText(Path.Combine(folder, "crash-20250101-000000-000.log"), "new");

        Assert.That(CrashLogStore.TakePendingReport(folder), Is.EqualTo("new"));
    }

    [Test]
    public void TakePendingReport_WhenReportTooLong_TruncatesToMaxLength()
    {
        var folder = CreateFolder();
        CrashLogStore.Write(folder, new string('x', 20));

        Assert.That(CrashLogStore.TakePendingReport(folder, 10), Is.EqualTo(new string('x', 10) + "\n..."));
    }

    [Test]
    public void TakePendingReport_KeepsHandledFileForRetrieval()
    {
        var folder = CreateFolder();
        CrashLogStore.Write(folder, "boom");
        CrashLogStore.TakePendingReport(folder);

        Assert.That(Directory.GetFiles(folder, "*.handled"), Has.Length.EqualTo(1));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using WizardScoreboard.Models;

namespace WizardScoreboard.Tests;

internal sealed class MemoryPreferences : IPreferences
{
    private readonly Dictionary<(string Container, string Key), object> values = new();

    public bool ContainsKey(string key, string? sharedName = null) => values.ContainsKey((sharedName ?? string.Empty, key));

    public void Remove(string key, string? sharedName = null) => values.Remove((sharedName ?? string.Empty, key));

    public void Clear(string? sharedName = null)
    {
        foreach (var key in values.Keys.Where(k => k.Container == (sharedName ?? string.Empty)).ToArray())
            values.Remove(key);
    }

    public void Set<T>(string key, T value, string? sharedName = null)
    {
        if (value is null)
            Remove(key, sharedName);
        else
            values[(sharedName ?? string.Empty, key)] = value;
    }

    public T Get<T>(string key, T defaultValue, string? sharedName = null) =>
        values.TryGetValue((sharedName ?? string.Empty, key), out var value) ? (T)value : defaultValue;
}

internal sealed class TestDeviceDisplay : IDeviceDisplay
{
    private bool keepScreenOn;
    internal bool IsAvailable { get; set; } = true;
    internal int SetCount { get; private set; }

    public bool KeepScreenOn
    {
        get
        {
            if (!IsAvailable)
                throw new InvalidOperationException("Device display is unavailable.");
            return keepScreenOn;
        }
        set
        {
            if (!IsAvailable)
                throw new InvalidOperationException("Device display is unavailable.");
            keepScreenOn = value;
            SetCount++;
        }
    }

    public DisplayInfo MainDisplayInfo => default;
    public event EventHandler<DisplayInfoChangedEventArgs> MainDisplayInfoChanged
    {
        add { }
        remove { }
    }
}

internal static class TestData
{
    internal static Group CreateGroup(int playerCount = 3) => new()
    {
        Name = "Test group",
        Players = Enumerable.Range(0, playerCount)
            .Select(index => new Player { Name = $"Player {index + 1}", Order = index })
            .ToList()
    };

    internal static Dictionary<Guid, int> Actuals(ScoreSession session, params int[] values) =>
        session.Players.Select((player, index) => (player.Id, Value: values[index]))
            .ToDictionary(pair => pair.Id, pair => pair.Value);
}

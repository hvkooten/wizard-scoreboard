using System;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using WizardScoreboard.Models;
using WizardScoreboard.Services;

namespace WizardScoreboard.Tests;

public class GroupServiceTests
{
    [Test]
    public void Constructor_NullPreferencesThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new GroupService(null!));
    }

    [Test]
    public void GetSelectedGroup_WithoutGroupsReturnsNull()
    {
        Assert.That(new GroupService(new MemoryPreferences()).GetSelectedGroup(), Is.Null);
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void CreateGroup_PersistsAllGroupAndPlayerData(int playerCount)
    {
        var preferences = new MemoryPreferences();
        var service = new GroupService(preferences);
        var group = service.CreateGroup("Saved group", TestData.CreateGroup(playerCount).Players);
        var expected = JsonSerializer.Serialize(group);

        var restored = new GroupService(preferences).GetGroups().Single();

        Assert.That(JsonSerializer.Serialize(restored), Is.EqualTo(expected));
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(7)]
    public void CreateGroup_RejectsUnsupportedPlayerCount(int playerCount)
    {
        var service = new GroupService(new MemoryPreferences());

        Assert.Throws<ArgumentException>(() => service.CreateGroup("Invalid", TestData.CreateGroup(playerCount).Players));
    }

    [Test]
    public void CreateGroup_OrdersPlayers()
    {
        var service = new GroupService(new MemoryPreferences());
        var players = TestData.CreateGroup().Players;
        players.Reverse();

        var group = service.CreateGroup("Ordered", players);

        Assert.That(group.Players.Select(p => p.Order), Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void CreateGroup_DoesNotReorderInputList()
    {
        var service = new GroupService(new MemoryPreferences());
        var players = TestData.CreateGroup().Players;
        players.Reverse();

        service.CreateGroup("Ordered", players);

        Assert.That(players.Select(p => p.Order), Is.EqualTo(new[] { 2, 1, 0 }));
    }

    [Test]
    public void CreateGroup_SelectsNewGroup()
    {
        var service = new GroupService(new MemoryPreferences());
        service.CreateGroup("First", TestData.CreateGroup().Players);

        var second = service.CreateGroup("Second", TestData.CreateGroup().Players);

        Assert.That(service.GetSelectedGroup(), Is.SameAs(second));
    }

    [Test]
    public void SetSelectedGroup_PersistsSelection()
    {
        var preferences = new MemoryPreferences();
        var service = new GroupService(preferences);
        var first = service.CreateGroup("First", TestData.CreateGroup().Players);
        service.CreateGroup("Second", TestData.CreateGroup().Players);

        service.SetSelectedGroup(first.Id);

        Assert.That(new GroupService(preferences).GetSelectedGroup()?.Id, Is.EqualTo(first.Id));
    }

    [Test]
    public void SetSelectedGroup_UnknownIdKeepsSelection()
    {
        var service = new GroupService(new MemoryPreferences());
        var group = service.CreateGroup("Selected", TestData.CreateGroup().Players);

        service.SetSelectedGroup(Guid.NewGuid());

        Assert.That(service.GetSelectedGroup(), Is.SameAs(group));
    }

    [Test]
    public void GetGroup_ReturnsMatchingGroup()
    {
        var service = new GroupService(new MemoryPreferences());
        var group = service.CreateGroup("Existing", TestData.CreateGroup().Players);

        Assert.That(service.GetGroup(group.Id), Is.SameAs(group));
    }

    [Test]
    public void GetGroup_UnknownIdReturnsNull()
    {
        var service = new GroupService(new MemoryPreferences());

        Assert.That(service.GetGroup(Guid.NewGuid()), Is.Null);
    }

    [Test]
    public void UpdateGroup_PersistsRenamedGroup()
    {
        var preferences = new MemoryPreferences();
        var service = new GroupService(preferences);
        var group = service.CreateGroup("Original", TestData.CreateGroup().Players);

        service.UpdateGroup(new Group { Id = group.Id, Name = "Renamed", Players = group.Players });

        Assert.That(new GroupService(preferences).GetGroup(group.Id)?.Name, Is.EqualTo("Renamed"));
    }

    [Test]
    public void UpdateGroup_PersistsPlayersInOrder()
    {
        var preferences = new MemoryPreferences();
        var service = new GroupService(preferences);
        var group = service.CreateGroup("Original", TestData.CreateGroup().Players);
        var players = TestData.CreateGroup(4).Players;
        players.Reverse();

        service.UpdateGroup(new Group { Id = group.Id, Name = group.Name, Players = players });

        var restored = new GroupService(preferences).GetGroups().Single();
        Assert.That(restored.Players.Select(p => p.Order), Is.EqualTo(new[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void UpdateGroup_UnknownIdThrows()
    {
        var service = new GroupService(new MemoryPreferences());

        Assert.Throws<InvalidOperationException>(() => service.UpdateGroup(TestData.CreateGroup()));
    }

    [Test]
    public void DeleteGroup_SelectedGroupFallsBackToFirstRemainingGroup()
    {
        var preferences = new MemoryPreferences();
        var service = new GroupService(preferences);
        var first = service.CreateGroup("First", TestData.CreateGroup().Players);
        var second = service.CreateGroup("Second", TestData.CreateGroup().Players);

        service.DeleteGroup(second.Id);

        Assert.That(new GroupService(preferences).GetSelectedGroup()?.Id, Is.EqualTo(first.Id));
    }

    [Test]
    public void DeleteGroup_UnselectedGroupKeepsSelection()
    {
        var preferences = new MemoryPreferences();
        var service = new GroupService(preferences);
        var first = service.CreateGroup("First", TestData.CreateGroup().Players);
        var selected = service.CreateGroup("Selected", TestData.CreateGroup().Players);

        service.DeleteGroup(first.Id);

        Assert.That(new GroupService(preferences).GetSelectedGroup()?.Id, Is.EqualTo(selected.Id));
    }

    [Test]
    public void DeleteGroup_PersistsRemoval()
    {
        var preferences = new MemoryPreferences();
        var service = new GroupService(preferences);
        var group = service.CreateGroup("Deleted", TestData.CreateGroup().Players);

        service.DeleteGroup(group.Id);

        Assert.That(new GroupService(preferences).GetGroups(), Is.Empty);
    }

    [Test]
    public void DeleteGroup_LastGroupClearsSelection()
    {
        var preferences = new MemoryPreferences();
        var service = new GroupService(preferences);
        var group = service.CreateGroup("Deleted", TestData.CreateGroup().Players);

        service.DeleteGroup(group.Id);

        Assert.That(new GroupService(preferences).GetSelectedGroup(), Is.Null);
    }

    [Test]
    public void DeleteGroup_UnknownIdPreservesGroups()
    {
        var service = new GroupService(new MemoryPreferences());
        var group = service.CreateGroup("Preserved", TestData.CreateGroup().Players);

        service.DeleteGroup(Guid.NewGuid());

        Assert.That(service.GetGroups().Select(g => g.Id), Is.EqualTo(new[] { group.Id }));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("{broken")]
    [TestCase("{}")]
    public void NewService_EmptyOrMalformedStoredGroupsReturnsEmptyList(string raw)
    {
        var preferences = new MemoryPreferences();
        preferences.Set("groups_storage_v1", raw);

        Assert.That(new GroupService(preferences).GetGroups(), Is.Empty);
    }

    [TestCase("")]
    [TestCase("invalid-guid")]
    [TestCase("00000000-0000-0000-0000-000000000001")]
    public void NewService_MissingOrInvalidSelectionFallsBackToFirstGroup(string selection)
    {
        var preferences = new MemoryPreferences();
        var service = new GroupService(preferences);
        var first = service.CreateGroup("First", TestData.CreateGroup().Players);
        service.CreateGroup("Second", TestData.CreateGroup().Players);
        preferences.Set("selected_group_id_v1", selection);

        Assert.That(new GroupService(preferences).GetSelectedGroup()?.Id, Is.EqualTo(first.Id));
    }
}

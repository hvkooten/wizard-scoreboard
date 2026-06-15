using Microsoft.Maui.Storage;
using System.Text.Json;
using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

public class GroupService : IGroupService
{
    private const string GroupsStorageKey = "groups_storage_v1";
    private const string SelectedGroupStorageKey = "selected_group_id_v1";
    private readonly List<Group> groups = new();
    private Guid? selectedGroupId;

    public GroupService()
    {
        Load();
    }

    public IEnumerable<Group> GetGroups() => groups;

    public Group CreateGroup(string name, List<Player> players)
    {
        if (players.Count < 3 || players.Count > 6)
            throw new ArgumentException("Aantal spelers moet tussen 3 en 6 liggen.");

        var group = new Group
        {
            Name = name,
            Players = players.OrderBy(p => p.Order).ToList()
        };

        groups.Add(group);
        selectedGroupId = group.Id;
        Save();
        return group;
    }

    public void UpdateGroup(Group group)
    {
        var existing = groups.FirstOrDefault(x => x.Id == group.Id);
        if (existing == null)
            throw new InvalidOperationException("Groep bestaat niet.");

        existing.Name = group.Name;
        existing.Players = group.Players.OrderBy(p => p.Order).ToList();
        Save();
    }

    public void DeleteGroup(Guid groupId)
    {
        var group = groups.FirstOrDefault(x => x.Id == groupId);
        if (group != null)
        {
            groups.Remove(group);

            if (selectedGroupId == groupId)
            {
                selectedGroupId = groups.FirstOrDefault()?.Id;
            }

            Save();
        }
    }

    public Group? GetGroup(Guid groupId) => groups.FirstOrDefault(x => x.Id == groupId);

    public Group? GetSelectedGroup()
    {
        if (selectedGroupId == null)
        {
            return groups.FirstOrDefault();
        }

        return groups.FirstOrDefault(g => g.Id == selectedGroupId) ?? groups.FirstOrDefault();
    }

    public void SetSelectedGroup(Guid groupId)
    {
        if (groups.Any(g => g.Id == groupId))
        {
            selectedGroupId = groupId;
            Save();
        }
    }

    private void Load()
    {
        var rawGroups = Preferences.Default.Get(GroupsStorageKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(rawGroups))
        {
            try
            {
                var savedGroups = JsonSerializer.Deserialize<List<Group>>(rawGroups);
                if (savedGroups != null)
                {
                    groups.Clear();
                    groups.AddRange(savedGroups);
                }
            }
            catch (JsonException)
            {
                groups.Clear();
            }
        }

        var rawSelected = Preferences.Default.Get(SelectedGroupStorageKey, string.Empty);
        if (Guid.TryParse(rawSelected, out var parsedSelected))
        {
            selectedGroupId = parsedSelected;
        }

        if (selectedGroupId == null && groups.Count > 0)
        {
            selectedGroupId = groups[0].Id;
        }
    }

    private void Save()
    {
        var rawGroups = JsonSerializer.Serialize(groups);
        Preferences.Default.Set(GroupsStorageKey, rawGroups);
        Preferences.Default.Set(SelectedGroupStorageKey, selectedGroupId?.ToString() ?? string.Empty);
    }
}

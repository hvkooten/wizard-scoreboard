using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

public class GroupService : IGroupService
{
    private readonly List<Group> groups = new();

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
        return group;
    }

    public void UpdateGroup(Group group)
    {
        var existing = groups.FirstOrDefault(x => x.Id == group.Id);
        if (existing == null)
            throw new InvalidOperationException("Groep bestaat niet.");

        existing.Name = group.Name;
        existing.Players = group.Players.OrderBy(p => p.Order).ToList();
    }

    public void DeleteGroup(Guid groupId)
    {
        var group = groups.FirstOrDefault(x => x.Id == groupId);
        if (group != null)
            groups.Remove(group);
    }

    public Group? GetGroup(Guid groupId) => groups.FirstOrDefault(x => x.Id == groupId);
}

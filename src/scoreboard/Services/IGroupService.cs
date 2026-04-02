using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

public interface IGroupService
{
    IEnumerable<Group> GetGroups();
    Group CreateGroup(string name, List<Player> players);
    void UpdateGroup(Group group);
    void DeleteGroup(Guid groupId);
    Group? GetGroup(Guid groupId);
}

using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

public interface IHighscoreService
{
    IEnumerable<Player> GetHighscores();
    void UpdateHighscores(IEnumerable<Group> groups);
}

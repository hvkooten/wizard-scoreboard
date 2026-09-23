namespace WizardScoreboard.Models;

public class Group
{
    // Sentinel for BidTotalRuleStartRound meaning "follow the number of players".
    // Also used as the default so migrated groups keep the player-count behavior.
    public const int PlayerCountRule = -1;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Player> Players { get; set; } = new List<Player>();
    public int BidTotalRuleStartRound { get; set; } = PlayerCountRule; // follow player count until explicitly set
}

namespace WizardScoreboard.Models;

public class Group
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Player> Players { get; set; } = new List<Player>();
    public int BidTotalRuleStartRound { get; set; } = -1; // -1 means not set for migration
}

namespace WizardScoreboard.Models;

public class Group
{
    // Sentinel for BidTotalRuleStartRound meaning "follow the number of players" (starts at players + 1).
    // Also used as the default so migrated groups keep the player-count behavior.
    public const int PlayerCountRule = -1;

    // Sentinel for BidTotalRuleStartRound meaning "follow twice the number of players" (starts at 2 x players + 1).
    public const int DoublePlayerCountRule = -2;

    // Highest possible round number (3 players play 20 rounds).
    public const int MaxRoundLimit = 20;

    // Number of rounds in a game: the 60-card deck divided over the players (3-6 players).
    public static int MaxRounds(int playerCount) => 60 / Math.Clamp(playerCount, 3, 6);

    // Resolves the actual start round for the player-count based rules; the rule applies from the
    // round after the player count (e.g. 3 players: round 4, or round 7 for the double rule).
    public static int PlayerCountStartRound(int playerCount, bool doublePlayerCount) =>
        (doublePlayerCount ? playerCount * 2 : playerCount) + 1;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Player> Players { get; set; } = new List<Player>();
    public int BidTotalRuleStartRound { get; set; } = PlayerCountRule; // follow player count until explicitly set
    public bool AllowNoTrump { get; set; } = true;
}

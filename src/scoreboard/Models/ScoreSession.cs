namespace WizardScoreboard.Models;

public enum TrumpSuit
{
    None,
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

public class RoundEntry
{
    public int RoundNumber { get; set; }
    public Guid DealerPlayerId { get; set; }
    public Dictionary<Guid, int> BidByPlayer { get; set; } = new();
    public Dictionary<Guid, int> ActualByPlayer { get; set; } = new();
    public TrumpSuit Trump { get; set; }
}

public class ScoreSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public int CurrentRound { get; set; } = 0;
    public int MaxRounds { get; set; }
    public TrumpSuit Trump { get; set; } = TrumpSuit.None;
    public List<RoundEntry> Rounds { get; set; } = new();
    public bool IsActive { get; set; }
    public bool IsPaused { get; set; }
    public Guid CurrentDealer => Players.Count == 0 ? Guid.Empty : Players[CurrentRound % Players.Count].Id;
    public List<Player> Players { get; set; } = new();
    public int BidTotalRuleStartRound { get; set; } = 1;  // Starting round for total bids rule
}

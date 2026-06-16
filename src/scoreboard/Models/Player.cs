namespace WizardScoreboard.Models;

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int Wins { get; set; }
    public int GamesPlayed { get; set; }
    public int HighestScore { get; set; }
    public int Order { get; set; }
    public int CurrentPoints { get; set; }
}

namespace WizardScoreboard.Services;

public enum TrumpPaletteMode
{
    CardSuits,
    FourColors
}

public interface ITrumpPaletteService
{
    TrumpPaletteMode GetMode();
    void SetMode(TrumpPaletteMode mode);
    string[] GetTrumpLabels();
}

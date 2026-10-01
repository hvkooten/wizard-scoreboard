using WizardScoreboard.Resources;

namespace WizardScoreboard.Services;

public class TrumpPaletteService : ITrumpPaletteService
{
    private const string PaletteModeKey = "trump_palette_mode";
    private readonly IPreferences preferences;

    /// <summary>Creates the palette service using device preferences.</summary>
    public TrumpPaletteService() : this(Preferences.Default)
    {
    }

    /// <summary>Creates the palette service using the supplied preferences.</summary>
    public TrumpPaletteService(IPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        this.preferences = preferences;
    }

    public TrumpPaletteMode GetMode()
    {
        var raw = preferences.Get(PaletteModeKey, nameof(TrumpPaletteMode.CardSuits));
        return Enum.TryParse<TrumpPaletteMode>(raw, out var mode) ? mode : TrumpPaletteMode.CardSuits;
    }

    public void SetMode(TrumpPaletteMode mode)
    {
        preferences.Set(PaletteModeKey, mode.ToString());
    }

    public string[] GetTrumpLabels()
    {
        return GetMode() == TrumpPaletteMode.FourColors
            ? new[]
            {
                Localization.GetString("None"),
                Localization.GetString("Red"),
                Localization.GetString("Yellow"),
                Localization.GetString("Green"),
                Localization.GetString("Blue")
            }
            : new[]
            {
                Localization.GetString("None"),
                Localization.GetString("Hearts"),
                Localization.GetString("Diamonds"),
                Localization.GetString("Clubs"),
                Localization.GetString("Spades")
            };
    }
}

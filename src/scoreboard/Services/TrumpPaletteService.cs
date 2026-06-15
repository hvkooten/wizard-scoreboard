using Microsoft.Maui.Storage;
using WizardScoreboard.Resources;

namespace WizardScoreboard.Services;

public class TrumpPaletteService : ITrumpPaletteService
{
    private const string PaletteModeKey = "trump_palette_mode";

    public TrumpPaletteMode GetMode()
    {
        var raw = Preferences.Default.Get(PaletteModeKey, nameof(TrumpPaletteMode.CardSuits));
        return Enum.TryParse<TrumpPaletteMode>(raw, out var mode) ? mode : TrumpPaletteMode.CardSuits;
    }

    public void SetMode(TrumpPaletteMode mode)
    {
        Preferences.Default.Set(PaletteModeKey, mode.ToString());
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

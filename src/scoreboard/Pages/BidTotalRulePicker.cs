using WizardScoreboard.Models;
using WizardScoreboard.Resources;

namespace WizardScoreboard.Pages;

// Shared mapping for the bid-total-rule picker used in Settings (default) and Groups (per-group).
// Picker items are: [0]=Disabled, [1]=Player Count, [2]=2x Player Count, [3..]=rounds 1..max rounds.
// Stored value is: 0=Disabled, Group.PlayerCountRule=follow player count,
// Group.DoublePlayerCountRule=follow twice the player count, 1..Group.MaxRoundLimit=fixed round.
internal static class BidTotalRulePicker
{
    // Fills the picker with the fixed options followed by rounds 1..maxRound.
    public static void PopulateItems(Picker picker, int maxRound)
    {
        picker.Items.Clear();
        picker.Items.Add(Localization.GetString("Disabled"));
        picker.Items.Add(Localization.GetString("Player Count"));
        picker.Items.Add(Localization.GetString("DoublePlayerCount"));
        for (var round = 1; round <= maxRound; round++)
        {
            picker.Items.Add(round.ToString());
        }
    }

    public static int ValueFromIndex(int index) => index switch
    {
        <= 0 => 0,
        1 => Group.PlayerCountRule,
        2 => Group.DoublePlayerCountRule,
        _ => index - 2
    };

    public static int IndexFromValue(int value) => value switch
    {
        0 => 0,
        Group.PlayerCountRule => 1,
        Group.DoublePlayerCountRule => 2,
        < 0 or > Group.MaxRoundLimit => 1,
        _ => value + 2
    };
}

using WizardScoreboard.Models;

namespace WizardScoreboard.Pages;

// Shared mapping for the bid-total-rule picker used in Settings (default) and Groups (per-group).
// Picker items are: [0]=Disabled, [1]=Player Count, [2]=2x Player Count, [3..15]=rounds 1..13.
// Stored value is: 0=Disabled, Group.PlayerCountRule=follow player count,
// Group.DoublePlayerCountRule=follow twice the player count, 1..13=fixed round.
internal static class BidTotalRulePicker
{
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
        < 0 or > 13 => 1,
        _ => value + 2
    };
}

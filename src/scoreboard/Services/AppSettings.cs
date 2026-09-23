using Microsoft.Maui.Storage;
using WizardScoreboard.Models;

namespace WizardScoreboard.Services;

// App-wide defaults that are not tied to a specific group.
public static class AppSettings
{
    private const string DefaultBidTotalRuleKey = "default_bid_total_rule";

    // Default bid-total-rule start round applied when creating a new group.
    // Group.PlayerCountRule means "follow the number of players".
    public static int DefaultBidTotalRuleStartRound
    {
        get => Preferences.Default.Get(DefaultBidTotalRuleKey, Group.PlayerCountRule);
        set => Preferences.Default.Set(DefaultBidTotalRuleKey, value);
    }
}

using System.Globalization;
using System.Resources;

namespace WizardScoreboard.Resources;

public static class Localization
{
    private static readonly ResourceManager ResourceManager = new ResourceManager("WizardScoreboard.Resources.Resx.Strings", typeof(Localization).Assembly);

    public static string GetString(string key)
    {
        return ResourceManager.GetString(key, CultureInfo.CurrentCulture) ?? key;
    }

    public static void SetCulture(string cultureName)
    {
        CultureInfo culture = new CultureInfo(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}

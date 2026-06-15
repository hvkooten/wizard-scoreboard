using System.Globalization;
using System.Resources;

namespace WizardScoreboard.Resources;

public static class Localization
{
    private static readonly ResourceManager ResourceManager = new ResourceManager("WizardScoreboard.Resources.Resx.Strings", typeof(Localization).Assembly);
    private static readonly string[] SupportedCultures = { "nl-NL", "en-US", "de-DE", "es-ES", "fr-FR" };

    public static string GetString(string key)
    {
        return ResourceManager.GetString(key, CultureInfo.CurrentCulture) ?? key;
    }

    public static void InitializeCulture()
    {
        SetCulture(ResolveSupportedCulture(CultureInfo.CurrentUICulture.Name));
    }

    public static string ResolveSupportedCulture(string? cultureName)
    {
        if (!string.IsNullOrWhiteSpace(cultureName))
        {
            var exactMatch = SupportedCultures.FirstOrDefault(c =>
                string.Equals(c, cultureName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
            {
                return exactMatch;
            }

            try
            {
                var twoLetter = new CultureInfo(cultureName).TwoLetterISOLanguageName;
                var languageMatch = SupportedCultures.FirstOrDefault(c =>
                    c.StartsWith(twoLetter + "-", StringComparison.OrdinalIgnoreCase));
                if (languageMatch != null)
                {
                    return languageMatch;
                }
            }
            catch (CultureNotFoundException)
            {
                // Ignore and use fallback below.
            }
        }

        return "nl-NL";
    }

    public static string SetCulture(string cultureName)
    {
        var resolvedCultureName = ResolveSupportedCulture(cultureName);
        CultureInfo culture = new CultureInfo(resolvedCultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        return resolvedCultureName;
    }
}

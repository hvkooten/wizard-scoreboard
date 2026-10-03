using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard.Pages;

public class RulesPage : ContentPage
{
    public RulesPage(ITrumpPaletteService trumpPaletteService)
    {
        ArgumentNullException.ThrowIfNull(trumpPaletteService);
        this.trumpPaletteService = trumpPaletteService;
        PageTitleHelper.Apply(this, Localization.GetString("Rules"));

        rulesView = new WebView();
        Content = rulesView;
    }

    private readonly ITrumpPaletteService trumpPaletteService;
    private readonly WebView rulesView;
    private string? loadedLanguage;
    private AppTheme? loadedTheme;
    private TrumpPaletteMode? loadedPaletteMode;

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Application.Current is { } app)
        {
            app.RequestedThemeChanged -= OnRequestedThemeChanged;
            app.RequestedThemeChanged += OnRequestedThemeChanged;
        }

        await LoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (Application.Current is { } app)
        {
            app.RequestedThemeChanged -= OnRequestedThemeChanged;
        }
    }

    private async void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var theme = Application.Current?.RequestedTheme ?? AppTheme.Light;
        var paletteMode = trumpPaletteService.GetMode();
        if (language == loadedLanguage && theme == loadedTheme && paletteMode == loadedPaletteMode)
        {
            return;
        }

        var dark = theme == AppTheme.Dark;
        rulesView.BackgroundColor = dark ? Color.FromArgb("#10161d") : Colors.White;

        var html = await LoadRulesHtmlAsync(language);
        rulesView.Source = new HtmlWebViewSource { Html = ApplySuitStyle(ApplyTheme(html, dark), paletteMode) };
        loadedLanguage = language;
        loadedTheme = theme;
        loadedPaletteMode = paletteMode;
    }

    /// <summary>Hides the rules variant (colors or card suits) that does not match the chosen trump palette.</summary>
    public static string ApplySuitStyle(string html, TrumpPaletteMode mode)
    {
        var hiddenClass = mode == TrumpPaletteMode.CardSuits ? "suit-colors" : "suit-cards";
        var css = $"<style>.{hiddenClass}{{display:none !important;}}</style>";
        var index = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? html.Insert(index, css) : css + html;
    }

    private static string ApplyTheme(string html, bool dark)
    {
        var css = dark
            ? "<style>:root{color-scheme:dark;}html,body{background:#10161d !important;color:#e6e6e6 !important;}a{color:#7fb3ff;}</style>"
            : "<style>:root{color-scheme:light;}html,body{background:#ffffff !important;color:#111111 !important;}a{color:#0055cc;}</style>";

        var index = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? html.Insert(index, css) : css + html;
    }

    private static async Task<string> LoadRulesHtmlAsync(string language)
    {
        try
        {
            return await ReadAssetAsync($"rules/rules.{language}.html");
        }
        catch (IOException)
        {
            return await ReadAssetAsync("rules/rules.en.html");
        }
    }

    private static async Task<string> ReadAssetAsync(string path)
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}

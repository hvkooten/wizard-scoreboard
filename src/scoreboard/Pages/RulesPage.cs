using WizardScoreboard.Resources;

namespace WizardScoreboard.Pages;

public class RulesPage : ContentPage
{
    public RulesPage()
    {
        PageTitleHelper.Apply(this, Localization.GetString("Rules"));

        rulesView = new WebView();
        Content = rulesView;
    }

    private readonly WebView rulesView;
    private string? loadedLanguage;
    private AppTheme? loadedTheme;

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
        if (language == loadedLanguage && theme == loadedTheme)
        {
            return;
        }

        var dark = theme == AppTheme.Dark;
        rulesView.BackgroundColor = dark ? Color.FromArgb("#10161d") : Colors.White;

        var html = await LoadRulesHtmlAsync(language);
        rulesView.Source = new HtmlWebViewSource { Html = ApplyTheme(html, dark) };
        loadedLanguage = language;
        loadedTheme = theme;
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

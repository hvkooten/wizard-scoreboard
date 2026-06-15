using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using WizardScoreboard;
using WizardScoreboard.Pages;
using WizardScoreboard.Resources;
using WizardScoreboard.Services;

namespace WizardScoreboard;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Localization.InitializeCulture();

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<IGroupService, GroupService>();
        builder.Services.AddSingleton<IScoreService, ScoreService>();
        builder.Services.AddSingleton<IHighscoreService, HighscoreService>();
        builder.Services.AddSingleton<ITrumpPaletteService, TrumpPaletteService>();

        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<RulesPage>();
        builder.Services.AddTransient<HighscorePage>();
        builder.Services.AddTransient<ScoreBoardPage>();
        builder.Services.AddTransient<AppShell>();

        return builder.Build();
    }
}

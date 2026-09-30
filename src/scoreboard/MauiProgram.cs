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
                // Free/open-source medieval display font (SIL OFL) used for the "Wizard" title.
                fonts.AddFont("MedievalSharp.ttf", "WizardFont");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler<HeaderLabel, HeaderLabelHandler>();
#endif
            });

        builder.Services.AddSingleton<IGroupService, GroupService>();
        builder.Services.AddSingleton<IScoreService, ScoreService>();
        builder.Services.AddSingleton<IHighscoreService, HighscoreService>();
        builder.Services.AddSingleton<ITrumpPaletteService, TrumpPaletteService>();
        builder.Services.AddSingleton<IScreenWakeService, ScreenWakeService>();

        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AboutPage>();
        builder.Services.AddTransient<GroupsPage>();
        builder.Services.AddTransient<RulesPage>();
        builder.Services.AddTransient<HighscorePage>();
        builder.Services.AddTransient<SavedGamesPage>();
        builder.Services.AddTransient<ScoreBoardPage>();
        builder.Services.AddTransient<AppShell>();

        return builder.Build();
    }
}

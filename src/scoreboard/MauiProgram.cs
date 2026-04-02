using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using WizardScoreboard;
using WizardScoreboard.Services;

namespace WizardScoreboard;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
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

        return builder.Build();
    }
}

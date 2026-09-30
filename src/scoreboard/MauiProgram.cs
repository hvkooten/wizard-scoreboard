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
            });

#if ANDROID
        // MedievalSharp has no real bold weight. Android re-resolves the typeface whenever the font
        // or text is (re)mapped, which drops the synthesized bold and makes the header flip to
        // non-bold. Re-apply bold after every such mapping so the header stays consistently bold.
        Microsoft.Maui.Handlers.LabelHandler.Mapper.AppendToMapping(nameof(ILabel.Font), ForceHeaderBold);
        Microsoft.Maui.Handlers.LabelHandler.Mapper.AppendToMapping(nameof(ILabel.Text), ForceHeaderBold);
#endif

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

#if ANDROID
    private static void ForceHeaderBold(Microsoft.Maui.Handlers.ILabelHandler handler, ILabel label)
    {
        if (label is not HeaderLabel)
        {
            return;
        }

        var textView = handler.PlatformView;
        textView.SetTypeface(textView.Typeface, Android.Graphics.TypefaceStyle.Bold);
        textView.Paint.FakeBoldText = true;
    }
#endif
}

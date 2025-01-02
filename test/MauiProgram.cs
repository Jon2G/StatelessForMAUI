using SampleApp.Pages;
using StatelessForMAUI;

namespace SampleApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var debug = false;
#if DEBUG
        debug = true;
#endif
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .UseMauiCommunityToolkitMediaElement()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .UseStatelessForMaui(typeof(SplashPage), debug: debug);

        return builder.Build();
    }
}

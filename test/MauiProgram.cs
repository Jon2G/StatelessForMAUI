using StatelessForMAUI;
namespace SampleApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .UseMauiCommunityToolkitMediaElement()
            .UseMauiCommunityToolkit()
            .UseStatelessForMauiApp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<MainViewModel>();

        builder.Services.AddSingleton<MainPage>();

        builder.Services.AddSingleton<WebViewViewModel>();

        builder.Services.AddSingleton<WebViewPage>();

        builder.Services.AddSingleton<MediaElementViewModel>();

        builder.Services.AddSingleton<MediaElementPage>();

        builder.Services.AddSingleton<MapViewModel>();

        builder.Services.AddSingleton<MapPage>();

        builder.Services.AddSingleton<DrawingViewModel>();

        builder.Services.AddSingleton<DrawingPage>();

        return builder.Build();
    }
}

using SampleApp.Pages;

namespace SampleApp;

public partial class App : StatelessForMAUI.StatelessForMauiApp
{
    public App()
    {
        InitializeComponent();
        var debug = false;
#if DEBUG
        debug = true;
#endif

        Initialize(typeof(SplashPage), debug: debug);
    }
}

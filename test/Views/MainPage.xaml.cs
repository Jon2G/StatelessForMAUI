using StatelessForMAUI.Attributes;
using StatelessForMAUI.StateMachine;

namespace SampleApp.Views;

[StatelessNavigation(isRoot: true, canGoBack: false, allowedTransitions:
    [typeof(DrawingPage), typeof(MapPage), typeof(MediaElementPage), typeof(WebViewPage)]
    )]
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new MainViewModel();
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        NavigationStateMachine.GoTo<DrawingPage>();
    }

    private void Button_Clicked_1(object sender, EventArgs e)
    {
        NavigationStateMachine.GoTo<MapPage>();
    }

    private void Button_Clicked_2(object sender, EventArgs e)
    {
        NavigationStateMachine.GoTo<MediaElementPage>();
    }

    private void Button_Clicked_3(object sender, EventArgs e)
    {
        NavigationStateMachine.GoTo<WebViewPage>();
    }
}

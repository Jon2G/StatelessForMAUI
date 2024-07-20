using StatelessForMAUI.Attributes;
using StatelessForMAUI.StateMachine;
namespace SampleApp.Pages;

[StatelessNavigation(allowedTransitions: [
    typeof(MainPage)
    ])]
public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(2000); // Simulate a long loading time
        NavigationStateMachine.GoTo<MainPage>();
    }
}
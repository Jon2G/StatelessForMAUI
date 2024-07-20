using StatelessForMAUI.Attributes;

namespace SampleApp.Views;
[StatelessNavigation(GoBackTarget: typeof(MainPage))]
public partial class WebViewPage : ContentPage
{
    public WebViewPage()
    {
        InitializeComponent();
        BindingContext = new WebViewViewModel();
    }
}

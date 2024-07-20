using StatelessForMAUI.Attributes;

namespace SampleApp.Views;
[StatelessNavigation(GoBackTarget: typeof(MainPage))]
public partial class MapPage : ContentPage
{
    public MapPage()
    {
        InitializeComponent();
        BindingContext = new MapViewModel();
#if WINDOWS
		// Note that the map control is not supported on Windows.
		// For more details, see https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/map?view=net-maui-7.0
		// For a possible workaround, see https://github.com/CommunityToolkit/Maui/issues/605
		Content = new Label() { Text = "Windows does not have a map control. 😢" };
#else
        Content = new Microsoft.Maui.Controls.Maps.Map();
#endif
    }
}

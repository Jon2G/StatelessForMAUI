using StatelessForMAUI.Attributes;

namespace SampleApp.Views;
[StatelessNavigation(GoBackTarget: typeof(MainPage))]
public partial class MediaElementPage : ContentPage
{
    public MediaElementPage()
    {
        // TODO: change the source and add your own controls for playback as necessary.
        InitializeComponent();
        BindingContext = new MediaElementViewModel();
    }

    void Page_Unloaded(object sender, EventArgs e)
    {
        // Stop and cleanup MediaElement when we navigate away
        mediaElement.Handler?.DisconnectHandler();
    }
}

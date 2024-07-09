namespace SampleApp.ViewModels;

public partial class MediaElementViewModel : BaseViewModel
{
	[ObservableProperty]
	public string source = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";
}

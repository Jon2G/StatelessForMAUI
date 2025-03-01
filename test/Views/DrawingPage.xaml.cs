using System.Windows.Input;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using StatelessForMAUI.Attributes;
using StatelessForMAUI.StateMachine;

namespace SampleApp.Views;
[StatelessNavigation(GoBackTarget: typeof(MainPage), permitReentry: true, canGoBack: true)]
public partial class DrawingPage : ContentPage
{
    public DrawingViewModel ViewModel => BindingContext as DrawingViewModel;
    public DrawingPage()
    {
        InitializeComponent();
    }

    private void Button_Clicked(object sender, EventArgs e)
    {

    }

    private void AnotherPageClicked(object sender, EventArgs e)
    {
        NavigationStateMachine.GoTo<DrawingPage, DrawingViewModel>(new() { NestedLevel = this.ViewModel.NestedLevel + 1 });
    }
}
public class DrawingViewModel : BaseViewModel
{
    [ObservableProperty]
    public ObservableCollection<IDrawingLine> Lines { get; set; } = new();

    [ObservableProperty]
    public int NestedLevel { get; set; } = 1;

    [ObservableProperty]
    public ICommand ClearCommand { get; private set; }

    public DrawingViewModel()
    {
        ClearCommand = new Command(Clear);
    }

    private void Clear()
    {
        Lines.Clear();
    }


}
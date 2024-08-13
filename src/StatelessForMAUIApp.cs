using StatelessForMAUI.StateMachine;
using StatelessForMAUI.StateMachine.Triggers;
using TinyTypeContainer;

namespace StatelessForMAUI
{
    public class StatelessForMauiApp : Application
    {
        private static INavigation? navigation;
        public static INavigation? Navigation
        {
            get => navigation ??= Application.Current?.MainPage?.Navigation;
            internal set
            {
                navigation = value;
            }
        }
        public static Page? RootPage { get; internal set; }

        //internal static ConnectivityStateMachine? ConnectivityStateMachine { get; private set; }
        //internal static NavigationStateMachine? NavigationStateMachine { get; private set; }
        public static AppLifeStateMachine Instance
        {
            get => Container.GetRequired<AppLifeStateMachine>();
        }
        internal static bool Debug { get; private set; }
        public Page Initialize(Type? splashPageType = null,
            Type? onDisconnectedFromInternet = null,
            Type? onNetworkError = null,
            bool HapticFeedBackOnPageChange = false,
            bool debug = false)
        {
            StatelessForMauiApp.Debug= debug;
            var splashPage = ActivatePage(splashPageType);
            splashPage.Appearing += SplashPage_Appearing;

            Container.Register(new ConnectivityStateMachine(
   onDisconnectedFromInternet: onDisconnectedFromInternet,
   onNetworkError: onNetworkError
   ));
            Container.Register(new NavigationStateMachine(splashPageType, HapticFeedBackOnPageChange));
            NavigationStateMachine.CurrentPage = splashPage;
            Container.Register(new AppLifeStateMachine());
            NavigationPage.SetHasNavigationBar(splashPage, false);
            MainPage = new NavigationPage(splashPage);
            Navigation = MainPage.Navigation;
            RootPage = MainPage;
            NavigationStateMachine.OnNavigatedTo(splashPage, string.Empty);
            return MainPage;
        }

        private void SplashPage_Appearing(object? sender, EventArgs e)
        {
            if (sender is Page sp)
            {
                sp.Appearing += SplashPage_Appearing;
            }
            AppLifeStateMachine.Fire(AppLifeTrigger.OnStart);
        }

        internal static Page ActivatePage(Type? type)
        {
            if (type == null)
            {
                return new ContentPage();
            }
            if (type.IsAbstract)
            {
                throw new InvalidOperationException("Type must be a concrete class");
            }
            if (!type.IsSubclassOf(typeof(Page)))
            {
                if (type.IsSubclassOf(typeof(View)))
                {
                    return new ContentPage() { Content = (View)Activator.CreateInstance(type)! };
                }
                throw new InvalidOperationException("Type must be a subclass of Page");
            }
            return (Page)Activator.CreateInstance(type)!;

        }

        protected override void OnResume()
        {
            base.OnResume();
            AppLifeStateMachine.Instance.StateMachine.Fire(AppLifeTrigger.OnResume);
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            AppLifeStateMachine.Instance.StateMachine.Fire(AppLifeTrigger.OnBackground);
        }

    }
}

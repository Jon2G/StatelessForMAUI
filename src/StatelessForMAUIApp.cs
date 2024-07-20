﻿using StatelessForMAUI.StateMachine;
using StatelessForMAUI.StateMachine.Triggers;

namespace StatelessForMAUI
{
    public class StatelessForMauiApp : Application
    {
        private static INavigation? navigation;
        public static INavigation Navigation
        {
            get => navigation ??= Application.Current!.MainPage!.Navigation;
            internal set
            {
                navigation = value;
            }
        }

        internal static ConnectivityStateMachine? ConnectivityStateMachine { get; private set; }
        internal static NavigationStateMachine? NavigationStateMachine { get; private set; }
        internal static AppLifeStateMachine? AppLifeStateMachine { get; private set; }
        internal static bool Debug { get; private set; }
        public Page Initialize(Type? splashPageType = null,
            Type? onDisconnectedFromInternet = null,
            Type? onNetworkError = null,
            bool HapticFeedBackOnPageChange = false,
            bool debug = false)
        {
            Debug = debug;
            var splashPage = ActivatePage(splashPageType);
            splashPage.Appearing += SplashPage_Appearing;

            ConnectivityStateMachine = new ConnectivityStateMachine(
   onDisconnectedFromInternet: onDisconnectedFromInternet,
   onNetworkError: onNetworkError
   );
            NavigationStateMachine = new NavigationStateMachine(splashPageType, HapticFeedBackOnPageChange);
            NavigationStateMachine.OnNavigatedTo(splashPage, string.Empty);
            NavigationStateMachine.CurrentPage = splashPage;
            AppLifeStateMachine = new AppLifeStateMachine();
            NavigationPage.SetHasNavigationBar(splashPage, false);
            return MainPage = new NavigationPage(splashPage);
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

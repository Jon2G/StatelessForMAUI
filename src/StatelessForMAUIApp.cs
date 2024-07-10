using Microsoft.Extensions.DependencyInjection;
using Stateless.Graph;
using StatelessForMAUI.StateMachine;
using StatelessForMAUI.StateMachine.Triggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatelessForMAUI
{
    public class StatelessForMAUIApp : Application
    {
        private static INavigation navigation;
        public static INavigation Navigation
        {
            get => navigation ??= Application.Current!.MainPage!.Navigation;
            internal set
            {
                navigation = value;
            }
        }


        private Type? SplashPageType;
        private Type? OnDisconectedFromInternetType;
        private Type? OnNetworkErrorType;
        internal static ConectivityStateMachine ConectivityStateMachine { get; private set; }
        internal static NavigationStateMachine NavigationStateMachine { get; private set; }
        internal static AppLifeStateMachine AppLifeStateMachine { get; private set; }

        public Page Initialize(Type? splashPageType = null,
            Type? onDisconectedFromInternet = null,
            Type? onNetworkError = null, bool debug = false)
        {
            SplashPageType = splashPageType;
            OnDisconectedFromInternetType = onDisconectedFromInternet;
            OnNetworkErrorType = onNetworkError;
            var splashPage = ActivatePage(SplashPageType);
            splashPage.Appearing += SplashPage_Appearing;

            ConectivityStateMachine = new ConectivityStateMachine(
   onDisconectedFromInternet: onDisconectedFromInternet,
   onNetworkError: onNetworkError
   );
            NavigationStateMachine = new NavigationStateMachine(SplashPageType);
            AppLifeStateMachine = new AppLifeStateMachine();
#if WINDOWS
            makeDiagrams();
#endif
            return new FlyoutPage()
            {
                Detail = splashPage,
                ////
                Flyout = new ContentPage() { Title = "flyout" },
            }; ;
        }

        private void SplashPage_Appearing(object? sender, EventArgs e)
        {
            if (sender is Page sp)
            {
                sp.Appearing += SplashPage_Appearing;
            }
            StatelessForMAUIApp.AppLifeStateMachine.Fire(AppLifeTrigger.OnStart);
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
            if (type.IsSubclassOf(typeof(Page)) == false)
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

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            AppLifeStateMachine.Instance.StateMachine.Fire(AppLifeTrigger.OnBackground);
        }

#if WINDOWS
        private void makeDiagrams()
        {

            string graph = string.Empty;

            var directory = new DirectoryInfo(
                "C:\\Users\\jonathgarcia\\Documents\\VS\\Nominas\\Docs\\Diagramas"
            );

            File.WriteAllText(directory.FullName + "/AppLifeStateMachine.dot", graph);

            graph = UmlDotGraph.Format(NavigationStateMachine.StateMachine.GetInfo());
            File.WriteAllText(directory.FullName + "/NavigationStateMachine.dot", graph);

            graph = UmlDotGraph.Format(ConectivityStateMachine.StateMachine.GetInfo());
            File.WriteAllText(directory.FullName + "/ConectivityStateMachine.dot", graph);

            graph = UmlDotGraph.Format(AppLifeStateMachine.StateMachine.GetInfo());
            File.WriteAllText(directory.FullName + "/AppLifeStateMachine.dot", graph);
        }
#endif
    }
}

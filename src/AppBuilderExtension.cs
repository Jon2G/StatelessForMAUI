
using System.Reflection;
using System.Runtime.CompilerServices;
using KeyboardVisibilityListener;
using Microsoft.Maui.LifecycleEvents;
using StatelessForMAUI.Attributes;
using StatelessForMAUI.StateMachine;
using StatelessForMAUI.StateMachine.Triggers;
using TinyTypeContainer;

namespace StatelessForMAUI
{
    public static class AppBuilderExtension
    {
        private static bool IsInitializated { get; set; }
        public static async Task<Page> Initialize(Type? splashPageType = null)
        {
            try
            {
                var splashPage = await NavigationStateMachine.ActivatePage(type:splashPageType,pageParams:null);
                NavigationStateMachine.CurrentPage = splashPage;
                NavigationPage.SetHasNavigationBar(splashPage.CurrentPage, false);
                Application.Current!.MainPage = splashPage;
                AppLifeStateMachine.Navigation = Application.Current.MainPage.Navigation;
                AppLifeStateMachine.RootPage = Application.Current.MainPage;
                NavigationStateMachine.OnNavigatedTo(splashPage, string.Empty);
                return Application.Current.MainPage;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
        private static void OnCreate(Type? splashPageType = null,
            Type? onDisconnectedFromInternet = null,
            Type? onNetworkError = null,
            bool HapticFeedBackOnPageChange = false,
            bool debug = false)
        {
            if (!Container.Has<AppLifeStateMachine>())
            {
                AppLifeStateMachine.IsDebug = debug;
                Container.Register(new ConnectivityStateMachine(
                    onDisconnectedFromInternet: onDisconnectedFromInternet,
                    onNetworkError: onNetworkError
                ));
                Container.Register(new NavigationStateMachine(splashPageType, HapticFeedBackOnPageChange));
                Container.Register(new AppLifeStateMachine());
            }
            AppLifeStateMachine.Fire(AppLifeTrigger.OnStart);
        }
        private static void OnStart(Type? splashPageType = null)
        {
            AppLifeStateMachine.Fire(AppLifeTrigger.OnInitialized);
            if (!IsInitializated)
            {
                Initialize(splashPageType).ConfigureAwait(true);
                IsInitializated = true;
            }
        }
        private static void OnBackground() => AppLifeStateMachine.Fire(AppLifeTrigger.OnBackground);
        private static void OnResume() => AppLifeStateMachine.Fire(AppLifeTrigger.OnResume);
        private static bool OnBackPressed()
        {
            if (KeyboardVisibilityState.Instance.IsKeyboardOpen)
            {
                KeyBoardUtils.ForceCloseKeyboard();
                return true;
            }
            //NavigationStateMachine.GoBack();
            return true;
        }


        public static MauiAppBuilder UseStatelessForMaui(this MauiAppBuilder builder,
            Type? splashPageType = null,
            Type? onDisconnectedFromInternet = null,
            Type? onNetworkError = null,
            bool HapticFeedBackOnPageChange = false,
            bool debug = false)
        {

            splashPageType ??= FindSplashPageTypeByAttribute(Assembly.GetCallingAssembly());
            builder
    .ConfigureLifecycleEvents(events =>
    {
#if ANDROID
        events.AddAndroid(android => android
        //.OnActivityResult((activity, requestCode, resultCode, data) => LogEvent(nameof(AndroidLifecycle.OnActivityResult), requestCode.ToString()))
        .OnCreate((activity, bundle) =>_OnCreate())
        .OnStart((activity) => OnStart(splashPageType: splashPageType))
        .OnBackPressed((activity) => OnBackPressed())
            //.OnStop((activity) => AppLifeStateMachine.Fire(AppLifeTrigger.))
            .OnSaveInstanceState((activity, bundle) => OnBackground())
            .OnRestoreInstanceState((activity, bundle) => OnResume())
            .OnPause((activity) => OnBackground())
            .OnResume((activity) => OnResume())
            )
;
#elif IOS || MACCATALYST
        events.AddiOS(ios => ios
            .OnActivated((app) =>
                {
                   _OnCreate();
                    OnStart(splashPageType: splashPageType);
                }
                )

            //AppLifeStateMachine.Fire(AppLifeTrigger.OnInitialized))
            .SceneOnActivated((app) =>
                {
                    LogEvent(nameof(iOSLifecycle.SceneOnActivated));
                }

            )
            .OnResignActivation((app) =>
            {
                LogEvent(nameof(iOSLifecycle.OnResignActivation));
            })
            .DidEnterBackground((app) => OnBackground())
            .WillEnterForeground((app) => OnResume())
            .WillTerminate((app) =>
            {
                LogEvent(nameof(iOSLifecycle.WillTerminate));
            })
            );
        //.WillTerminate((app) => LogEvent(nameof(iOSLifecycle.WillTerminate))));
#elif WINDOWS
        events.AddWindows(windows => windows
               .OnActivated((window, args) => OnStart(splashPageType: splashPageType))
               //.OnClosed((window, args) => LogEvent(nameof(WindowsLifecycle.OnClosed)))
               //.OnLaunched((window, args) => LogEvent(nameof(WindowsLifecycle.OnLaunched)))
               .OnLaunching((window, args) =>_OnCreate())
               .OnVisibilityChanged((window, args) =>
               {
                   if (window.Visible)
                   {
                       OnResume();
                   }
                   else
                   {
                       OnBackground();
                   }
               })
               .OnPlatformMessage((window, args) =>
               {
                   if (args.MessageId == Convert.ToUInt32("031A", 16))
                   {
                       // System theme has changed
                   }
               }));
#endif
        static bool LogEvent(string eventName, string type = null)
        {
            Console.WriteLine($"Lifecycle event: {eventName}{(type == null ? string.Empty : $" ({type})")}");
            return true;
        }

    });
            
            
            void _OnCreate()
            {
               
                OnCreate(
                    splashPageType: splashPageType,
                    onNetworkError: onNetworkError,
                    onDisconnectedFromInternet: onDisconnectedFromInternet,
                    debug: debug,
                    HapticFeedBackOnPageChange: HapticFeedBackOnPageChange);
            }
            return builder;
        }

        private static Type? FindSplashPageTypeByAttribute(Assembly assembly)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                var attributes = type.GetCustomAttributes(typeof(SplashPageAttribute), true);
                if (attributes.Length > 0)
                {
                    return type;
                }
            }
            return null;
        }
    }
}

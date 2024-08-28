using AsyncAwaitBestPractices;
using KeyboardVisibilityListener;
using Microsoft.Maui.LifecycleEvents;
using StatelessForMAUI.StateMachine;
using StatelessForMAUI.StateMachine.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                var splashPage = await NavigationStateMachine.ActivatePage(splashPageType);
                NavigationStateMachine.CurrentPage = splashPage;
                NavigationPage.SetHasNavigationBar(splashPage, false);
                Application.Current!.MainPage = new NavigationPage(splashPage);
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
        public static MauiAppBuilder UseStatelessForMaui(this MauiAppBuilder builder,
            Type? splashPageType = null,
            Type? onDisconnectedFromInternet = null,
            Type? onNetworkError = null,
            bool HapticFeedBackOnPageChange = false,
            bool debug = false)
        {
            builder
    .ConfigureLifecycleEvents(events =>
    {
#if ANDROID
        events.AddAndroid(android => android
        //.OnActivityResult((activity, requestCode, resultCode, data) => LogEvent(nameof(AndroidLifecycle.OnActivityResult), requestCode.ToString()))
        .OnCreate((activity, bundle) =>
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
        })
        .OnStart((activity) =>
        {
            AppLifeStateMachine.Fire(AppLifeTrigger.OnInitialized);
            if (!IsInitializated)
            {
                Initialize(splashPageType).ConfigureAwait(true);
                IsInitializated = true;
            }
        })

            .OnBackPressed((activity) =>
            {
                if (KeyboardVisibilityState.Instance.IsKeyboardOpen)
                {
                    KeyBoardUtils.ForceCloseKeyboard();
                    return true;
                }
                NavigationStateMachine.GoBack();
                return true;
            })
            //.OnStop((activity) => AppLifeStateMachine.Fire(AppLifeTrigger.))
            .OnSaveInstanceState((activity, bundle) => AppLifeStateMachine.Fire(AppLifeTrigger.OnBackground))
            .OnRestoreInstanceState((activity, bundle) => AppLifeStateMachine.Fire(AppLifeTrigger.OnResume))
            .OnPause((activity) => AppLifeStateMachine.Fire(AppLifeTrigger.OnBackground))
            .OnResume((activity) => AppLifeStateMachine.Fire(AppLifeTrigger.OnResume))
            )
;
#elif IOS || MACCATALYST
                    events.AddiOS(ios => ios
                        .OnActivated((app) => AppLifeStateMachine.Fire(AppLifeTrigger.OnInitialized))
                        .SceneOnActivated((app)=> AppLifeStateMachine.Fire(AppLifeTrigger.OnStart))
                        //.OnResignActivation((app) => LogEvent(nameof(iOSLifecycle.OnResignActivation)))
                        .DidEnterBackground((app) => AppLifeStateMachine.Fire(AppLifeTrigger.OnBackground))
                        .WillEnterForeground((app)=>AppLifeStateMachine.Fire(AppLifeTrigger.OnResume))
                        );
                        //.WillTerminate((app) => LogEvent(nameof(iOSLifecycle.WillTerminate))));
#elif WINDOWS
                    events.AddWindows(windows => windows
                           .OnActivated((window, args) => LogEvent(nameof(WindowsLifecycle.OnActivated)))
                           .OnClosed((window, args) => LogEvent(nameof(WindowsLifecycle.OnClosed)))
                           .OnLaunched((window, args) => LogEvent(nameof(WindowsLifecycle.OnLaunched)))
                           .OnLaunching((window, args) => LogEvent(nameof(WindowsLifecycle.OnLaunching)))
                           .OnVisibilityChanged((window, args) => LogEvent(nameof(WindowsLifecycle.OnVisibilityChanged)))
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
            System.Diagnostics.Debug.WriteLine($"Lifecycle event: {eventName}{(type == null ? string.Empty : $" ({type})")}");
            return true;
        }

    });
            return builder;
        }
    }
}

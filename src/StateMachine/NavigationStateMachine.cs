using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Stateless;
using StatelessForMAUI.Attributes;
using StatelessForMAUI.Pages;
using static System.TimeZoneInfo;

namespace StatelessForMAUI.StateMachine
{
    internal class NavigationStateItem(
        string name,
        string triggerName,
        StatelessNavigationAttribute statelessNavigationAttribute,
        Type type
        )
    {
        public readonly string Name = name;
        public readonly string Trigger = triggerName;
        public readonly StatelessNavigationAttribute StatelessNavigationAttribute = statelessNavigationAttribute;
        public readonly Type Type = type;

        internal Page GetPage() => StatelessForMauiApp.ActivatePage(this.Type);

        internal void BuildState(
            StateMachine<string, string> stateMachine,
            ConnectivityStateMachine? connectivityStateMachine = null
        )
        {
            if (StatelessForMauiApp.Debug)
            {
                Debug.WriteLine("BuildState " + Name);
            }
            if (StatelessNavigationAttribute.canGoBack)
            {
                if (StatelessNavigationAttribute.goBackTarget is not null)
                {
                    stateMachine
                        .Configure(this.Name)
                        .Permit(
                            NavigationStateMachine.GO_BACK,
                            PageStateNameGenerator.GetPageStateName(
                                StatelessNavigationAttribute.goBackTarget
                            )
                        );
                }
                else
                {
                    stateMachine
                        .Configure(this.Name)
                        .PermitDynamicAsync(
                            NavigationStateMachine.GO_BACK,
                            NavigationStateMachine.DynamicGoBack
                        );
                }
            }
            else
            {
                stateMachine.Configure(this.Name).Ignore(NavigationStateMachine.GO_BACK);
            }
            if (StatelessNavigationAttribute.allowedTransitions is not null)
            {
                foreach (var transition in StatelessNavigationAttribute.allowedTransitions)
                {
                    var transitionName = PageStateNameGenerator.GetPageStateName(transition);
                    var _triggerName = PageStateNameGenerator.GetPageTrigger(transition);
                    stateMachine.Configure(this.Name).Permit(_triggerName, transitionName);
                }
            }

            if (StatelessNavigationAttribute.ignoredTransitions is not null)
            {
                foreach (var transition in StatelessNavigationAttribute.ignoredTransitions)
                {
                    var transitionName = PageStateNameGenerator.GetPageStateName(transition);
                    var _triggerName = PageStateNameGenerator.GetPageTrigger(transition);
                    stateMachine.Configure(this.Name).Permit(_triggerName, transitionName);
                }
            }
            if (
                StatelessNavigationAttribute.useNetworkTriggers
                && connectivityStateMachine is not null
            )
            {
                if (connectivityStateMachine.OnDisconnectedFromInternetPage is not null)
                {
                    stateMachine
                        .Configure(this.Name)
                        .Permit(
                            ConnectivityStateMachine.ON_DISCONECTED_FROM_INTERNET,
                            PageStateNameGenerator.GetPageStateName(
                                connectivityStateMachine.OnDisconnectedFromInternetPage
                            )
                        );
                }
                if (connectivityStateMachine.OnNetworkErrorPage is not null)
                {
                    stateMachine
                        .Configure(this.Name)
                        .Permit(
                            ConnectivityStateMachine.ON_NETWORK_ERROR,
                            PageStateNameGenerator.GetPageStateName(
                                connectivityStateMachine.OnNetworkErrorPage
                            )
                        );
                }
            }

            if (StatelessNavigationAttribute.onConnectedToInternet is not null)
            {
                stateMachine
                    .Configure(this.Name)
                    .Permit(
                        ConnectivityStateMachine.ON_CONNECTED_TO_INTERNET,
                        PageStateNameGenerator.GetPageStateName(
                            StatelessNavigationAttribute.onConnectedToInternet
                        )
                    );
            }

            if (StatelessNavigationAttribute.permitReentry)
            {
                stateMachine.Configure(this.Name).PermitReentry(this.Trigger);
            }

            if (StatelessNavigationAttribute.selfIgnore)
            {
                stateMachine.Configure(this.Name).Ignore(this.Trigger);
            }
        }
    }

    public class NavigationStateMachine : StateMachineBase<string, string>
    {
        public const string GO_BACK = "GoBack";
        private static INavigation Navigation => StatelessForMauiApp.Navigation;
        public static NavigationStateMachine Instance
        {
            get =>
                Application.Current!.Handler.MauiContext!.Services.GetRequiredService<NavigationStateMachine>();
        }
        public override StateMachine<string, string> StateMachine { get; protected set; }

        public static Page? CurrentPage { get; internal set; } = null;
        private ReadOnlyDictionary<string, Func<Page>>? Pages;

        public NavigationStateMachine(Type? splashPageType)
        {
            this.StateMachine = new StateMachine<string, string>(
                PageStateNameGenerator.GetPageStateName(splashPageType)
            );
            this.StateMachine.OnUnhandledTrigger((state, trigger) =>
            {
                if (StatelessForMauiApp.Debug)
                {
                    Debug.WriteLine($"Unhandled trigger {trigger} in state {state}");
                }
                if (this.StateMachine.CanFire(GO_BACK))
                {
                    Fire(GO_BACK);
                }
            });
            this.BuildStateMachine(StatelessForMauiApp.ConnectivityStateMachine);
        }

        private void BuildStateMachine(ConnectivityStateMachine? connectivityStateMachine = null)
        {
            var pagesDictionary = new Dictionary<string, Func<Page>>();
            foreach (
                Type type in AppDomain
                    .CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .SelectMany(a =>
                        a.GetTypes()
                            .Where(x => !x.IsAbstract && x.IsSubclassOf(typeof(Page)))
                    )
            )
            {
                var attribute = type.GetCustomAttribute<StatelessNavigationAttribute>(false);
                if (attribute is not null)
                {
                    string name = PageStateNameGenerator.GetPageStateName(type);
                    string triggerName = PageStateNameGenerator.GetPageTrigger(type);
                    var navigationItem = new NavigationStateItem(
                        name,
                        triggerName,
                        attribute,
                        type
                    );
                    pagesDictionary.Add(name, () => navigationItem.GetPage());
                    navigationItem.BuildState(this.StateMachine, connectivityStateMachine);
                }
            }
            Pages = new ReadOnlyDictionary<string, Func<Page>>(pagesDictionary);
            Init();
        }

        internal static async Task<string> DynamicGoBack()
        {
            string backPageName = string.Empty;
            await MainThread.InvokeOnMainThreadAsync(async () =>
             {
                 INavigation navigation = Navigation;
                 if (navigation.ModalStack.Count > 0)
                 {
                     Page? away = await navigation.PopModalAsync();
                     var navigationCollection = navigation.ModalStack ?? navigation.NavigationStack;
                     CurrentPage = navigationCollection.Count > 0 ? navigationCollection[navigationCollection.Count - 1] : Application.Current?.MainPage;
                     OnNavigatedAway(away, CurrentPage);
                     OnNavigatedTo(CurrentPage, away);
                     backPageName = CurrentPage!.GetPageStateName();
                 }
                 else
                 {
                     backPageName = NavigationStateMachine.CurrentPage!.GetPageStateName();
                 }
             }); if (StatelessForMauiApp.Debug)
            {
                Debug.WriteLine($"GoBack to ${backPageName}");
            }
            return backPageName;
        }

        public static NavigationStateMachine GoTo<T>()
            where T : Page
        {
            return Fire(PageStateNameGenerator.GetPageTrigger(typeof(T)));
        }

        public static NavigationStateMachine Fire(string trigger)
        {
            Debug.WriteLineIf(StatelessForMauiApp.Debug,
                "Fire: " + trigger
            );
            var instance = NavigationStateMachine.Instance;
            instance.StateMachine.FireAsync(trigger).SafeFireAndForget();
            return instance;
        }

        public static Task<NavigationStateMachine> FireAsync(string trigger)
        {
            var instance = NavigationStateMachine.Instance;
            return instance.StateMachine.FireAsync(trigger).ContinueWith(t => instance);
        }

        public static bool FireIfYouCan(string trigger)
        {
            if (Instance.StateMachine.CanFire(trigger))
            {
                Fire(trigger);
                return true;
            }
            return false;
        }

        public static void GoBack()
        {
            Application
            .Current?.Dispatcher.DispatchAsync(() => Fire(GO_BACK))
            .SafeFireAndForget();
        }

        internal static void OnNavigatedAway(Page? away, string? to)
        {
            if (away is INavigationEventsPage awayPage)
            {
                awayPage.OnNavigatedAway(to);
            }
        }

        internal static void OnNavigatedAway(Page? away, Page? to)
        {
            OnNavigatedAway(away, to?.GetPageStateName());
        }

        internal static void OnNavigatedTo(Page? to, string? from)
        {
            if (to is INavigationEventsPage toPage)
            {
                toPage.OnNavigatedTo(from ?? string.Empty);
            }
        }

        internal static void OnNavigatedTo(Page? to, Page from)
        {
            OnNavigatedTo(to, from.GetPageStateName());
        }

        private static Page BuildPage(Func<Page> func)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                if (StatelessForMauiApp.Debug)
                {
                    Debug.WriteLine(
                        "Failed to create an instance of page:" + func.GetType().GenericTypeArguments[0]
                    );
                    Debug.WriteLine(ex);
                }
                throw;
            }
        }

        private void Init()
        {
            this.StateMachine.OnTransitionedAsync(async t =>
            {
                Page? page = null;
                MainThread
                    .InvokeOnMainThreadAsync(
                        () => HapticFeedback.Default.Perform(HapticFeedbackType.Click)
                    )
                    .SafeFireAndForget();
                bool isShell = Shell.Current is not null;
                if (StatelessForMauiApp.Debug)
                {
                    Console.WriteLine(t.Trigger + " <-> " + t.Source.ToString() + "->" + t.Destination);
                }

                if (isShell)
                {
                    Shell.Current!.FlyoutIsPresented = false;
                }

                if (t.Trigger == GO_BACK)
                {
                    if (t.Destination == CurrentPage?.GetPageStateName())
                    {
                        return;
                    }
                    INavigation navigation = Navigation;
                    if (navigation.ModalStack.Count > 0)
                    {
                        var away = await navigation.PopModalAsync();
                        OnNavigatedAway(away, t.Destination);
                        var navigationCollection = navigation.ModalStack;
                        CurrentPage = navigationCollection.Count > 0 ? navigationCollection[navigationCollection.Count - 1] : Application.Current?.MainPage;
                        OnNavigatedTo(CurrentPage, t.Source);
                        return;
                    }
                }
                if (Pages is not null && Pages.TryGetValue(t.Destination, out Func<Page>? value))
                {
                    page = BuildPage(value);
                }
                if (page is null)
                {
                    if (StatelessForMauiApp.Debug)
                    {
                        Debug.WriteLine("page is null!", "Error");
                    }
                    return;
                }
                var attribute = page.GetType().GetCustomAttribute<StatelessNavigationAttribute>();
                OnNavigatedAway(CurrentPage, t.Destination);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (StatelessForMauiApp.Debug)
                    {
                        Debug.WriteLine("Dispatched");
                    }
                    CurrentPage = page;
                    if (attribute?.isRoot ?? false)
                    {
                        await PushRootPage(page);
                        OnNavigatedTo(page, t.Source);
                        return;
                    }
                    await Navigation.PushModalAsync(page);
                    OnNavigatedTo(page, t.Source);
                });
            });
        }

        private static async Task PushRootPage(Page page)
        {
            await Task.Yield();
            bool popToRoot = false;
            if (Application.Current!.MainPage!.GetType() == page.GetType())
            {
                popToRoot = true;
            }
            else
            {
                Application.Current!.MainPage = page;
            }

            if (
                Application.Current!.MainPage is FlyoutPage flyout
                && flyout.Detail.Navigation is INavigation flyNavigation
            )
            {
                StatelessForMauiApp.Navigation = flyNavigation;
            }
            if (popToRoot)
            {
                await StatelessForMauiApp.Navigation.PopToRootAsync();
            }
        }
    }
}

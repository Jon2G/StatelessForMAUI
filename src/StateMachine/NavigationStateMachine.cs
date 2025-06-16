using AsyncAwaitBestPractices;
using Microsoft.Maui.Platform;
using Stateless;
using StatelessForMAUI.Attributes;
using StatelessForMAUI.Pages;
using System.Collections.ObjectModel;
using System.Reflection;
using TinyTypeContainer;

namespace StatelessForMAUI.StateMachine
{
    internal class NavigationEvent
    {
        public readonly string? Trigger;
        public Page? Source;
        public readonly Type Destination;
        public readonly object[] Parameters;
        public NavigationEvent(string? trigger, Page? source, Type destination, params object[] parameters)
        {
            Trigger = trigger;
            Source = source;
            Destination = destination;
            Parameters = parameters;
        }
    }

    internal class NavigationStateItem(
        string name,
        string triggerName,
        StatelessNavigationAttribute statelessNavigationAttribute,
        Type type
    )
    {
        public readonly string Name = name;
        public readonly string Trigger = triggerName;
        public readonly StatelessNavigationAttribute StatelessNavigationAttribute =
            statelessNavigationAttribute;
        public readonly Type Type = type;

        internal Task<StatelessNavigationPage> GetPage(Dictionary<string, object>? pageParams) => NavigationStateMachine.ActivatePage(this.Type, pageParams);

        internal void BuildState(
            StateMachine<string, string> stateMachine,
            ConnectivityStateMachine? connectivityStateMachine = null
        )
        {
            if (AppLifeStateMachine.IsDebug)
            {
                Console.WriteLine("BuildState " + Name);
            }
            if (StatelessNavigationAttribute.canGoBack)
            {
                if (StatelessNavigationAttribute.goBackTarget is not null && StatelessNavigationAttribute.permitReentry == false)
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
            else if (StatelessNavigationAttribute.selfIgnore)
            {
                stateMachine.Configure(this.Name).Ignore(this.Trigger);
            }
        }
    }

    public class NavigationStateMachine
    {
        public const string GO_BACK = "GoBack";
        public static NavigationStateMachine Instance
        {
            get => Container.GetRequired<NavigationStateMachine>();
        }


        public static Page? CurrentPage { get; internal set; } = null;

        private readonly bool HapticFeedBack;

        public NavigationStateMachine(Type? splashPageType, bool hapticFeedBack)
        {
            this.HapticFeedBack = hapticFeedBack;
        }
        internal static async Task<StatelessNavigationPage> ActivatePage(Type? type, Dictionary<string, object>? pageParams = null)
        {
            if (type == null)
            {
                return new StatelessNavigationPage(new ContentPage());
            }
            if (type.IsAbstract)
            {
                throw new InvalidOperationException("Type must be a concrete class");
            }
            if (!type.IsSubclassOf(typeof(Page)))
            {
                throw new InvalidOperationException("Type must be a subclass of Page");
            }
            return await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ContentPage? page;
                if (pageParams is not null)
                {
                    page = ((ContentPage)Activator.CreateInstance(type, args: (object[])pageParams.Values.ToArray())!);
                }
                else
                {
                    page = ((ContentPage)Activator.CreateInstance(type)!);
                }
                return new StatelessNavigationPage(page);
            });
        }

        private static INavigation SetEmptyRootPage()
        {
            var navigationPage = new StatelessNavigationPage(new ContentPage());
            if (Application.Current is not null)
            {
                Application.Current.MainPage = navigationPage;
            }
            AppLifeStateMachine.RootPage = navigationPage;
            AppLifeStateMachine.Navigation = navigationPage.Navigation;
            return navigationPage.Navigation;
        }

        private static Page GetCurrentPage(INavigation navigation)
        {
            IReadOnlyList<Page> navigationCollection;
            if (navigation.ModalStack is not null && navigation.ModalStack.Count > 0)
            {
                navigationCollection = navigation.ModalStack;
            }
            else
            {
                navigationCollection = navigation.NavigationStack;
            }
            if (navigationCollection.Count > 0)
            {
                return navigationCollection[^1];
            }
            else if (Application.Current?.MainPage is NavigationPage nav)
            {
                return GetCurrentPage(nav.Navigation);
            }
            else
            {
                return Application.Current?.MainPage;
            }
        }
        internal static async Task<string> PopPage(Func<Task<Page?>> PopAction, INavigation navigation)
        {
            Page? away = await PopAction();
            CurrentPage = GetCurrentPage(navigation);
            OnNavigatedAway(away, CurrentPage);
            OnNavigatedTo(CurrentPage, away);
            return CurrentPage!.GetPageStateName();
        }

        internal static async Task PopPageFixed(Func<Task<Page?>> PopAction, INavigation navigation, NavigationEvent t)
        {
            Page? away = await PopAction();
            CurrentPage = GetCurrentPage(navigation);
            OnNavigatedAway(away, CurrentPage);
            OnNavigatedTo(CurrentPage, away);
        }

        internal static async Task FixedGoBack(NavigationEvent transition)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                INavigation navigation = EnsureNavigationPageIsSet();

                if (navigation.ModalStack.Count > 0)
                {
                    await PopPageFixed(() => navigation.PopModalAsync(), navigation, transition);
                }
                else if (navigation.NavigationStack.Count > 0)
                {
                    await PopPageFixed(() => navigation.PopAsync(), navigation, transition);
                }
            });


        }
        internal static async Task<string> DynamicGoBack()
        {
            string backPageName = string.Empty;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                INavigation navigation = EnsureNavigationPageIsSet();
                if (navigation.ModalStack.Count > 0)
                {
                    backPageName = await PopPage(() => navigation.PopModalAsync(), navigation);
                }
                else if (navigation.NavigationStack.Count > 0)
                {
                    backPageName = await PopPage(() => navigation.PopAsync(), navigation);
                }
                else
                {
                    backPageName = NavigationStateMachine.CurrentPage!.GetPageStateName();
                }
            });
            if (AppLifeStateMachine.IsDebug)
            {
                Console.WriteLine($"GoBack to ${backPageName}");
            }
            return backPageName;
        }
        public static Task<NavigationStateMachine> GoToAsync<T>() where T : Page => FireAsync(trigger: null, destination: typeof(T), param: null);

        public static Task<NavigationStateMachine> GoToAsync<T, V>()
            where T : Page
            where V : new()
        {
            return Instance.PerformNavigation(destination: typeof(T), trigger: null, args: new V()).ContinueWith(t => Instance);
        }
        public static Task<NavigationStateMachine> GoToAsync<T, V>(V? param)
            where T : Page
        {
            return Instance.PerformNavigation(destination: typeof(T), trigger: null, args: param).ContinueWith(t => Instance);
        }

        internal static NavigationStateMachine Fire(string? trigger = null, Type? destination = null, object? param = null)
        {
            if (AppLifeStateMachine.IsDebug)
            {
                Console.WriteLine("Fire: " + trigger);
            }
            var instance = NavigationStateMachine.Instance;
            instance.PerformNavigation(trigger: trigger, destination: destination, args: [param]).SafeFireAndForget();
            return instance;
        }
        internal static Task<NavigationStateMachine> FireAsync(string? trigger = null, Type? destination = null, object? param = null)
        {
            var instance = NavigationStateMachine.Instance;
            return instance.PerformNavigation(trigger: trigger, destination: destination, args: [param]).ContinueWith(t => instance);
        }

        public static bool CanGoBack()
        {
            throw new NotImplementedException("CanGoBack is not implemented");
        }

        public static void GoBack()
        {
            Application.Current?.Dispatcher.DispatchAsync(() => Fire(trigger: GO_BACK)).SafeFireAndForget();
        }
        public static async Task GoBackAsync()
        {
            await Application.Current?.Dispatcher.DispatchAsync(() => Fire(trigger: GO_BACK));
        }

        internal static void OnNavigatedAway(Page? away, Page? to)
        {
            if (away is INavigationEventsPage awayPage)
            {
                awayPage.OnNavigatedAway(to?.UnBoxStatelessNavigationPage());
            }
        }

        internal static void OnNavigatedTo(Page? to, Page? from)
        {
            if (to is INavigationEventsPage toPage)
            {
                toPage.OnNavigatedTo(from?.UnBoxStatelessNavigationPage());
            }
        }


        private static Task<StatelessNavigationPage> BuildPage(Type type, Dictionary<string, object>? pageParams)
        {
            try
            {
                return MainThread.InvokeOnMainThreadAsync(() => ActivatePage(type, pageParams));
            }
            catch (Exception ex)
            {
                if (AppLifeStateMachine.IsDebug)
                {
                    Console.WriteLine(
                        "Failed to create an instance of page:"
                            + type.GenericTypeArguments[0]
                    );
                    Console.WriteLine(ex);
                }
                throw;
            }
        }

        private bool IsInTransition;

        private Task PerformNavigation(string? trigger, Type destination, params object[] args) =>
            PerformNavigation(
                new NavigationEvent(trigger: trigger, destination: destination,
                    source: CurrentPage,
                    parameters: args
                )
            );
        private async Task PerformNavigation(NavigationEvent t)
        {
            while (IsInTransition)
            {
                await Task.Delay(100);
            }
            IsInTransition = true;
            try
            {
                if (t.Source is null)
                {
                    t.Source = CurrentPage;
                }
                StatelessNavigationPage? statelessNavigationPage = null;
                Page? page = null;
                if (this.HapticFeedBack)
                {
                    MainThread
                        .InvokeOnMainThreadAsync(
                            () => HapticFeedback.Default.Perform(HapticFeedbackType.Click)
                        )
                        .SafeFireAndForget();
                }
                bool isShell = Shell.Current is not null;
                if (AppLifeStateMachine.IsDebug)
                {
                    Console.WriteLine(
                        t.Trigger + " <-> " + t.Source.ToString() + "->" + t.Destination
                    );
                }

                if (isShell)
                {
                    Shell.Current!.FlyoutIsPresented = false;
                }

                if (t.Trigger == GO_BACK)
                {
                    EnsureNavigationPageIsSet();
                    await FixedGoBack(t);
                    return;
                }
                if (t.Destination is not null)
                {
                    statelessNavigationPage = await BuildPage(t.Destination, t.Parameters.Length == 2 ? t.Parameters[1] as Dictionary<string, object?> : null);
                    page = statelessNavigationPage?.CurrentPage;
                }
                if (statelessNavigationPage is null || page is null)
                {
                    if (AppLifeStateMachine.IsDebug)
                    {
                        Console.WriteLine("page is null!", "Error");
                    }
                    return;
                }
                if (t.Parameters.Length == 1 && t.Parameters[0] is not null)
                {
                    page.BindingContext = t.Parameters[0];
                }
                var attribute = page.GetType().GetCustomAttribute<StatelessNavigationAttribute>();
                OnNavigatedAway(CurrentPage, page);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (AppLifeStateMachine.IsDebug)
                    {
                        Console.WriteLine("Dispatched");
                    }
                    CurrentPage = page;
                    if (attribute?.isRoot ?? false)
                    {
                        await PushRootPage(statelessNavigationPage);
                        OnNavigatedTo(page, t.Source);
                        return;
                    }
                    if (attribute?.isModal ?? false)
                    {
                        await EnsureNavigationPageIsSet().PushModalAsync(statelessNavigationPage);
                    }
                    else
                    {
                        await EnsureNavigationPageIsSet().PushAsync(statelessNavigationPage);
                    }
                    OnNavigatedTo(page, t.Source);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                IsInTransition = false;
            }

        }

        private static INavigation EnsureNavigationPageIsSet()
        {
            return AppLifeStateMachine.Navigation ?? SetEmptyRootPage();
        }

        private static async Task PushRootPage(StatelessNavigationPage page)
        {
            await Task.Yield();
            bool popToRoot = false;
            if (Application.Current!.MainPage is StatelessNavigationPage currentPage && currentPage.GetType() == page.CurrentPage.GetType())
            {
                popToRoot = true;
            }
            else
            {
                if (page is NavigationPage navPage)
                {
                    Application.Current!.MainPage = navPage;
                }
                else
                {
                    Application.Current!.MainPage = new StatelessNavigationPage(page);
                }
                AppLifeStateMachine.Navigation = Application.Current!.MainPage.Navigation;

                AppLifeStateMachine.RootPage = page;
            }

            if (
                Application.Current!.MainPage is FlyoutPage flyout
                && flyout.Detail.Navigation is INavigation flyNavigation
            )
            {
                AppLifeStateMachine.RootPage = flyout;
                AppLifeStateMachine.Navigation = flyNavigation;
            }
            if (popToRoot && AppLifeStateMachine.Navigation is not null)
            {
                await AppLifeStateMachine.Navigation.PopToRootAsync();
            }
        }
    }
}

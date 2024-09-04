using AsyncAwaitBestPractices;
using Stateless;
using StatelessForMAUI.Attributes;
using StatelessForMAUI.Pages;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using TinyTypeContainer;

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
        public readonly StatelessNavigationAttribute StatelessNavigationAttribute =
            statelessNavigationAttribute;
        public readonly Type Type = type;

        internal Task<Page> GetPage() => NavigationStateMachine.ActivatePage(this.Type);

        internal void BuildState(
            StateMachine<string, string> stateMachine,
            ConnectivityStateMachine? connectivityStateMachine = null
        )
        {
            if (AppLifeStateMachine.IsDebug)
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
        public static NavigationStateMachine Instance
        {
            get => Container.GetRequired<NavigationStateMachine>();
        }
        public override StateMachine<string, string> StateMachine { get; protected set; }

        public static Page? CurrentPage { get; internal set; } = null;
        private ReadOnlyDictionary<string, Func<Task<Page>>>? Pages;
        private readonly bool HapticFeedBack;

        public NavigationStateMachine(Type? splashPageType, bool hapticFeedBack)
        {
            this.HapticFeedBack = hapticFeedBack;
            this.StateMachine = new StateMachine<string, string>(
                PageStateNameGenerator.GetPageStateName(splashPageType)
            );
            this.StateMachine.OnUnhandledTrigger(
                (state, trigger) =>
                {
                    if (AppLifeStateMachine.IsDebug)
                    {
                        Debug.WriteLine($"Unhandled trigger {trigger} in state {state}");
                    }
                    if (this.StateMachine.CanFire(GO_BACK))
                    {
                        Fire(GO_BACK);
                    }
                }
            );
            this.BuildStateMachine(Container.GetRequired<ConnectivityStateMachine>());
        }
        internal static async Task<Page> ActivatePage(Type? type)
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
                throw new InvalidOperationException("Type must be a subclass of Page");
            }
            return await MainThread.InvokeOnMainThreadAsync(() =>
            {
                return (Page)Activator.CreateInstance(type)!;
            });
        }
        private void BuildStateMachine(ConnectivityStateMachine? connectivityStateMachine = null)
        {
            var pagesDictionary = new Dictionary<string, Func<Task<Page>>>();
            foreach (
                Type type in AppDomain
                    .CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .SelectMany(a =>
                        a.GetTypes().Where(x => !x.IsAbstract && x.IsSubclassOf(typeof(Page)))
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
            Pages = new ReadOnlyDictionary<string, Func<Task<Page>>>(pagesDictionary);
            Init();
        }

        private static INavigation SetEmptyRootPage()
        {
            var navigationPage = new NavigationPage(new ContentPage());
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
                return navigationCollection[navigationCollection.Count - 1];
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

        internal static async Task<string> DynamicGoBack()
        {
            string backPageName = string.Empty;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                INavigation navigation = AppLifeStateMachine.Navigation ?? SetEmptyRootPage();
                if (navigation.ModalStack.Count > 0)
                {
                    Page? away = await navigation.PopModalAsync();
                    CurrentPage = GetCurrentPage(navigation);
                    OnNavigatedAway(away, CurrentPage);
                    OnNavigatedTo(CurrentPage, away);
                    backPageName = CurrentPage!.GetPageStateName();
                }
                else
                {
                    backPageName = NavigationStateMachine.CurrentPage!.GetPageStateName();
                }
            });
            if (AppLifeStateMachine.IsDebug)
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
            Debug.WriteLineIf(AppLifeStateMachine.IsDebug, "Fire: " + trigger);
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

        public static bool CanGoBack()
        {
            return Instance.StateMachine.CanFire(GO_BACK);
        }

        public static void GoBack()
        {
            Application.Current?.Dispatcher.DispatchAsync(() => Fire(GO_BACK)).SafeFireAndForget();
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

        private static Task<Page> BuildPage(Func<Page> func)
        {
            try
            {
                return MainThread.InvokeOnMainThreadAsync(func);
            }
            catch (Exception ex)
            {
                if (AppLifeStateMachine.IsDebug)
                {
                    Debug.WriteLine(
                        "Failed to create an instance of page:"
                            + func.GetType().GenericTypeArguments[0]
                    );
                    Debug.WriteLine(ex);
                }
                throw;
            }
        }

        private bool IsInTransition;

        private void Init()
        {
            this.StateMachine.OnTransitionedAsync(async t =>
            {
                while (IsInTransition)
                {
                    await Task.Delay(100);
                }
                IsInTransition = true;
                try
                {
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
                        var navigation = AppLifeStateMachine.Navigation ?? SetEmptyRootPage();
                        if (t.Destination == CurrentPage?.GetPageStateName())
                        {
                            return;
                        }

                        if (navigation.ModalStack.Count > 0)
                        {
                            var away = await navigation.PopModalAsync();
                            OnNavigatedAway(away, t.Destination);
                            var navigationCollection = navigation.ModalStack;
                            CurrentPage =
                                navigationCollection.Count > 0
                                    ? navigationCollection[navigationCollection.Count - 1]
                                    : Application.Current?.MainPage;
                            OnNavigatedTo(CurrentPage, t.Source);
                            return;
                        }
                    }
                    if (
                        Pages is not null
                        && Pages.TryGetValue(t.Destination, out Func<Task<Page>>? value)
                    )
                    {
                        page = await BuildPage(value);
                    }
                    if (page is null)
                    {
                        if (AppLifeStateMachine.IsDebug)
                        {
                            Debug.WriteLine("page is null!", "Error");
                        }
                        return;
                    }
                    var attribute = page.GetType()
                        .GetCustomAttribute<StatelessNavigationAttribute>();
                    OnNavigatedAway(CurrentPage, t.Destination);
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (AppLifeStateMachine.IsDebug)
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
                        var navigation = AppLifeStateMachine.Navigation ?? SetEmptyRootPage();
                        await navigation.PushModalAsync(page);
                        OnNavigatedTo(page, t.Source);
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                finally
                {
                    IsInTransition = false;
                }
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
                if (page is NavigationPage navPage)
                {
                    Application.Current!.MainPage = navPage;
                }
                else
                {
                    Application.Current!.MainPage = new NavigationPage(page);
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

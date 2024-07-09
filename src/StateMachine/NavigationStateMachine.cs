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
using Stateless;
using StatelessForMAUI.Attributes;
using StatelessForMAUI.Pages;
using static System.TimeZoneInfo;

namespace StatelessForMAUI.StateMachine
{
    internal class NavigationStateItem
    {
        public readonly string Name;
        public readonly string Trigger;
        public readonly StatelessNavigationAttribute StatelessNavigationAttribute;
        public readonly Type Type;

        public NavigationStateItem(
            string name,
            string triggerName,
            StatelessNavigationAttribute statelessNavigationAttribute,
            Type type
        )
        {
            this.Name = name;
            this.Trigger = triggerName;
            this.StatelessNavigationAttribute = statelessNavigationAttribute;
            this.Type = type;
        }

        internal Page GetPage() => StatelessForMAUIApp.ActivatePage(this.Type);

        internal void BuildState(
            StateMachine<string, string> stateMachine,
            ConectivityStateMachine? conectivityStateMachine = null
        )
        {
            Debug.WriteLine(Name);
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
                        .PermitDynamic(
                            NavigationStateMachine.GO_BACK,
                            NavigationStateMachine.DynamicGoBack
                        );
                }
            }
            else
            {
                stateMachine
                        .Configure(this.Name).Ignore(NavigationStateMachine.GO_BACK); 
            }
            if (StatelessNavigationAttribute.allowedTransitions is not null)
            {
                foreach (var transition in StatelessNavigationAttribute.allowedTransitions)
                {
                    var transitionName = PageStateNameGenerator.GetPageStateName(transition);
                    var triggerName = PageStateNameGenerator.GetPageTrigger(transition);
                    stateMachine.Configure(this.Name).Permit(triggerName, transitionName);
                }
            }

            if (StatelessNavigationAttribute.ignoredTransitions is not null)
            {
                foreach (var transition in StatelessNavigationAttribute.ignoredTransitions)
                {
                    var transitionName = PageStateNameGenerator.GetPageStateName(transition);
                    var triggerName = PageStateNameGenerator.GetPageTrigger(transition);
                    stateMachine.Configure(this.Name).Permit(triggerName, transitionName);
                }
            }
            if (
                StatelessNavigationAttribute.useNetworkTriggers
                && conectivityStateMachine is not null
            )
            {
                if (conectivityStateMachine.OnDisconectedFromInternetPage is not null)
                {
                    stateMachine
                        .Configure(this.Name)
                        .Permit(
                            ConectivityStateMachine.ON_DISCONECTED_FROM_INTERNET,
                            PageStateNameGenerator.GetPageStateName(
                                conectivityStateMachine.OnDisconectedFromInternetPage
                            )
                        );
                }
                if (conectivityStateMachine.OnNetworkErrorPage is not null)
                {
                    stateMachine
                        .Configure(this.Name)
                        .Permit(
                            ConectivityStateMachine.ON_NETWORK_ERROR,
                            PageStateNameGenerator.GetPageStateName(
                                conectivityStateMachine.OnNetworkErrorPage
                            )
                        );
                }
            }

            if (StatelessNavigationAttribute.onConnectedToInternet is not null)
            {
                stateMachine
                    .Configure(this.Name)
                    .Permit(
                        ConectivityStateMachine.ON_CONNECTED_TO_INTERNET,
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

    public class NavigationStateMachine
    {
        public const string GO_BACK = "GoBack";
        private static INavigation Navigation => StatelessForMAUIApp.Navigation;
        public static NavigationStateMachine Instance
        {
            get =>
                Application.Current!.Handler.MauiContext!.Services.GetRequiredService<NavigationStateMachine>();
        }
        public readonly StateMachine<string, string> StateMachine;

        public static Page? CurrentPage { get; private set; } = null;
        private ReadOnlyDictionary<string, Func<Page>> Pages;

        public NavigationStateMachine(Type? splashPageType)
        {
            this.StateMachine = new StateMachine<string, string>(
                PageStateNameGenerator.GetPageStateName(splashPageType)
            );
            this.BuildStateMachine(StatelessForMAUIApp.ConectivityStateMachine);
        }

        private void BuildStateMachine(ConectivityStateMachine? conectivityStateMachine = null)
        {
            var pagesDictionary = new Dictionary<string, Func<Page>>();
            foreach (
                Type type in AppDomain
                    .CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .SelectMany(a =>
                        a.GetTypes()
                            .Where(x => x.IsAbstract == false && x.IsSubclassOf(typeof(Page)))
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
                    navigationItem.BuildState(this.StateMachine, conectivityStateMachine);
                }
            }
            Pages = new ReadOnlyDictionary<string, Func<Page>>(pagesDictionary);
            Init();
        }

        internal static string DynamicGoBack()
        {
            var stack = Navigation.NavigationStack ?? Navigation.ModalStack;
            if (stack.Count > 1)
            {
                var backTo = stack[stack.Count - 2];
                if (backTo is Page page)
                {
                    return page.GetPageStateName();
                }
            }
            return NavigationStateMachine.CurrentPage!.GetPageStateName();
        }

        public static NavigationStateMachine GoTo<T>() where T:Page
        {
            return Fire(PageStateNameGenerator.GetPageTrigger(typeof(T)));
        }

        public static NavigationStateMachine Fire(string trigger)
        {
            var instance = NavigationStateMachine.Instance;
            instance.StateMachine.Fire(trigger);
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
                Instance.StateMachine.Fire(trigger);
                return true;
            }
            return false;
        }

        public static void GoBack()
        {
            Application
                .Current?.Dispatcher.DispatchAsync(() => Instance.StateMachine.Fire(GO_BACK))
                .SafeFireAndForget();
        }

        private void Init()
        {
            this.StateMachine.OnTransitioned(t =>
            {
                Page? page = null;
                MainThread.BeginInvokeOnMainThread(
                    () => HapticFeedback.Default.Perform(HapticFeedbackType.Click)
                );
                Dictionary<string, object?>? data = null;
                if (
                    t.Parameters != null
                    && t.Parameters.Length >= 1
                    && t.Parameters[0] is Dictionary<string, object?> _data
                )
                {
                    data = _data;
                }
                bool isShell = Shell.Current is not null;
                Console.WriteLine(t.Trigger + " <-> " + t.Source.ToString() + "->" + t.Destination);

                if (isShell)
                {
                    Shell.Current!.FlyoutIsPresented = false;
                }


                if (t.Trigger == GO_BACK)
                {
                    INavigation navigation = Navigation;
                    if (navigation.NavigationStack.Count > 0)
                    {
                        navigation.PopAsync().SafeFireAndForget();
                        return;
                    }
                }
                if (Pages.ContainsKey(t.Destination))
                {
                    page = Pages[t.Destination]();
                }
                if (page is null)
                {
                    return;
                }

                MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    Debug.WriteLine("Dispatched");
                    CurrentPage = page;
                    var attributte = page.GetType().GetCustomAttribute<StatelessNavigationAttribute>();
                    if (attributte?.isRoot ?? false)
                    {
                        if (Application.Current!.MainPage!.GetType()== page.GetType())
                        {
                            await Navigation.PopToRootAsync();
                        }
                        Application.Current!.MainPage = page;
                        return;
                    }

                   
                    await Navigation.PushModalAsync(page);
                }).SafeFireAndForget();
            });
        }
    }
}

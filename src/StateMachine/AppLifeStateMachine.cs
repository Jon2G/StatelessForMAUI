using Stateless;
using StatelessForMAUI.Pages;
using StatelessForMAUI.StateMachine.States;
using StatelessForMAUI.StateMachine.Triggers;
using TinyTypeContainer;

namespace StatelessForMAUI.StateMachine
{
    public class AppLifeStateMachine : StateMachineBase<AppLifeState, AppLifeTrigger>
    {
        public static AppLifeStateMachine Instance => Container.GetRequired<AppLifeStateMachine>();
        public override StateMachine<AppLifeState, AppLifeTrigger> StateMachine { get; protected set; }

        internal static bool IsDebug { get; set; }
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
        public AppLifeStateMachine()
        {
            this.StateMachine = new(AppLifeState.Off);

            this.StateMachine.Configure(AppLifeState.Off)
                .Permit(AppLifeTrigger.OnBackground, AppLifeState.Background)
                .Permit(AppLifeTrigger.OnResume, AppLifeState.Resume)
                .Permit(AppLifeTrigger.OnStart, AppLifeState.CooldBoot);

            this.StateMachine.Configure(AppLifeState.CooldBoot)
                .Permit(AppLifeTrigger.OnStart, AppLifeState.Running)
                .Permit(AppLifeTrigger.OnInitialized, AppLifeState.Running)
                .Permit(AppLifeTrigger.OnBackground, AppLifeState.Background)
                .OnEntry(t => InitializeApp());

            this.StateMachine.Configure(AppLifeState.Resume)
                .PermitReentry(AppLifeTrigger.OnResume)
                .Permit(AppLifeTrigger.OnBackground, AppLifeState.Background);

            this.StateMachine.Configure(AppLifeState.Background)
                .Permit(AppLifeTrigger.OnResume, AppLifeState.Resume)
                 .Permit(AppLifeTrigger.OnStart, AppLifeState.Resume)
                .PermitReentry(AppLifeTrigger.OnBackground);

            this.StateMachine.Configure(AppLifeState.Running)
                .Ignore(AppLifeTrigger.OnStart)
                .Ignore(AppLifeTrigger.OnInitialized)
                .PermitReentry(AppLifeTrigger.OnResume)
                .Permit(AppLifeTrigger.OnBackground, AppLifeState.Background);
            if (IsDebug)
            {
                this.StateMachine.OnTransitioned(t =>
                {
                    Console.WriteLine(t.Source.ToString() + "->" + t.Destination);
                });
            }
            this.StateMachine.OnUnhandledTrigger((t, s) =>
            {
                if (IsDebug)
                {
                    Console.WriteLine($"Unhandled trigger {t} in state {s}");
                }
            });
            Init();
        }

        private void InitializeApp()
        {
            this.StateMachine.Fire(AppLifeTrigger.OnInitialized);
        }

        public static AppLifeStateMachine Fire(AppLifeTrigger AppLifeTrigger)
        {
            if (IsDebug)
            {
                Console.WriteLine($"Life state machine fire{AppLifeTrigger}");
            }
            Instance.StateMachine.Fire(AppLifeTrigger);
            return Instance;
        }

        private AppLifeStateMachine Init()
        {
            StateMachine.OnTransitionCompleted(t =>
            {
                if (IsDebug)
                {
                    Console.WriteLine($"Life state machine [{t.Trigger}] = {t.Source}->{t.Destination}");
                }
                if (NavigationStateMachine.CurrentPage is IAppLifeStatePage statePage)
                {
                    switch (t.Destination)
                    {
                        case AppLifeState.Resume:
                            statePage.OnResume();
                            break;
                        case AppLifeState.Background:
                            statePage.OnBackground();
                            break;
                    }
                }
            });
            return this;
        }


    }
}

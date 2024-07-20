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


        public AppLifeStateMachine()
        {
            this.StateMachine = new(AppLifeState.Off);

            this.StateMachine.Configure(AppLifeState.Off)
                .Permit(AppLifeTrigger.OnBackground, AppLifeState.Background)
                .Permit(AppLifeTrigger.OnStart, AppLifeState.CooldBoot);

            this.StateMachine.Configure(AppLifeState.CooldBoot)
                .Permit(AppLifeTrigger.OnInitialized, AppLifeState.Running)
                .OnEntry(t => InitializeApp());

            this.StateMachine.Configure(AppLifeState.Resume)
                .Permit(AppLifeTrigger.OnResume, AppLifeState.Running)
                .Permit(AppLifeTrigger.OnBackground, AppLifeState.Background);

            this.StateMachine.Configure(AppLifeState.Background)
                .Permit(AppLifeTrigger.OnResume, AppLifeState.Resume)
                 .Permit(AppLifeTrigger.OnStart, AppLifeState.Resume)
                .Ignore(AppLifeTrigger.OnBackground);

            this.StateMachine.Configure(AppLifeState.Running)
                .PermitReentry(AppLifeTrigger.OnResume)
                .Permit(AppLifeTrigger.OnBackground, AppLifeState.Background);
            if (StatelessForMauiApp.Debug)
            {
                this.StateMachine.OnTransitioned(t =>
                {
                    Console.WriteLine(t.Source.ToString() + "->" + t.Destination);
                });
            }
            Init();
        }

        private void InitializeApp()
        {
            this.StateMachine.Fire(AppLifeTrigger.OnInitialized);
        }

        public static AppLifeStateMachine Fire(AppLifeTrigger AppLifeTrigger)
        {
            Instance.StateMachine.Fire(AppLifeTrigger);
            return Instance;
        }
        private AppLifeStateMachine Init()
        {
            StateMachine.OnTransitionCompleted(t =>
            {
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

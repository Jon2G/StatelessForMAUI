using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StatelessForMAUI.StateMachine.States;
using StatelessForMAUI.StateMachine.Triggers;
using StatelessForMAUI.Pages;

namespace StatelessForMAUI.StateMachine
{
    public class AppLifeStateMachine
    {
        public static AppLifeStateMachine Instance => StatelessForMAUIApp.AppLifeStateMachine;
        public readonly StateMachine<AppLifeState, AppLifeTrigger> StateMachine;
       

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
                .Permit(AppLifeTrigger.OnBackground, AppLifeState.Background)
                .OnEntry(t => ResumeApp());

            this.StateMachine.Configure(AppLifeState.Background)
                .Permit(AppLifeTrigger.OnResume, AppLifeState.Resume)
                 .Permit(AppLifeTrigger.OnStart, AppLifeState.Resume)
                .Ignore(AppLifeTrigger.OnBackground);

            this.StateMachine.Configure(AppLifeState.Running)
                .OnEntry(t =>
                {
                    if (!t.IsReentry)
                    {
                        InitializedApp();
                    }
                })
                .PermitReentry(AppLifeTrigger.OnResume)
                .Permit(AppLifeTrigger.OnBackground, AppLifeState.Background);

            this.StateMachine.OnTransitioned(t =>
            {
                Console.WriteLine(t.Source.ToString() + "->" + t.Destination);
            });
            Init();
        }

        private void InitializeApp()
        {
            this.StateMachine.Fire(AppLifeTrigger.OnInitialized);
        }

        private void InitializedApp()
        {
            //Acciones a ejecutar después de inicializar app
        }

        private void ResumeApp()
        {
            //Acciones a ejecutar después de resumir app
        }
        public AppLifeStateMachine Fire(AppLifeTrigger AppLifeTrigger)
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

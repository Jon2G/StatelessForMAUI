﻿using StatelessForMAUI.StateMachine.States;
using StatelessForMAUI.StateMachine.Triggers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stateless;
using StatelessForMAUI.Pages;

namespace StatelessForMAUI.StateMachine
{
    public class ConectivityStateMachine
    {
        public const string ON_DISCONECTED_FROM_INTERNET = "OnDisconectedFromInternet";
        public const string ON_CONNECTED_TO_INTERNET = "OnConnectedToInternet";
        public const string ON_NETWORK_ERROR = "OnNetworkError";
        public static ConectivityStateMachine Instance
        {
            get =>Application.Current!.Handler.MauiContext!.Services!.GetRequiredService<ConectivityStateMachine>();
        }
        internal readonly Type? OnDisconectedFromInternetPage;
        internal readonly Type? OnNetworkErrorPage;
        public static bool HasInternet => Instance.StateMachine.State == ConectivityState.On;
        public readonly StateMachine<ConectivityState, ConectivityTrigger> StateMachine;

        public readonly ReadOnlyDictionary<NetworkAccess, ConectivityTrigger> NetworkAccessConectivity =
            new ReadOnlyDictionary<NetworkAccess, ConectivityTrigger>(new Dictionary<NetworkAccess, ConectivityTrigger>
            {
                { NetworkAccess.Unknown, ConectivityTrigger.OnNetworkTypeChanged },
                { NetworkAccess.None, ConectivityTrigger.OnDisconnected },
                { NetworkAccess.Local, ConectivityTrigger.OnConnected },
                { NetworkAccess.ConstrainedInternet, ConectivityTrigger.OnConnected },
                { NetworkAccess.Internet, ConectivityTrigger.OnConnected }
            });
        private readonly ReadOnlyDictionary<
    ConectivityTrigger,
    StateMachine<ConectivityState, ConectivityTrigger>.TriggerWithParameters<Dictionary<
        string,
        object?
        >?>
> _TriggerWithParameters;

        public ConectivityStateMachine(Type? onDisconectedFromInternet,
            Type? onNetworkError)
        {
            this.OnDisconectedFromInternetPage = onDisconectedFromInternet;
            this.OnNetworkErrorPage = onNetworkError;
            this.StateMachine = new(ConectivityState.Unknown);
            this.StateMachine.Configure(ConectivityState.Unknown)
                .Permit(ConectivityTrigger.OnDisconnected, ConectivityState.Off)
                .Ignore(ConectivityTrigger.OnNetworkTypeChanged)
                .Permit(ConectivityTrigger.NetworkError, ConectivityState.Error)
                .Permit(ConectivityTrigger.OnConnected, ConectivityState.On);

            this.StateMachine.Configure(ConectivityState.Off)
            .Ignore(ConectivityTrigger.OnDisconnected)
                .Ignore(ConectivityTrigger.OnNetworkTypeChanged)
                .Permit(ConectivityTrigger.OnConnected, ConectivityState.On);
            this.StateMachine.Configure(ConectivityState.On)
            .Ignore(ConectivityTrigger.OnConnected)
                .Permit(ConectivityTrigger.OnDisconnected, ConectivityState.Off)
                .Permit(ConectivityTrigger.NetworkError, ConectivityState.Error)
                .Ignore(ConectivityTrigger.OnNetworkTypeChanged);
            this.StateMachine.Configure(ConectivityState.Error)
                .Ignore(ConectivityTrigger.OnConnected)
                .Permit(ConectivityTrigger.OnDisconnected, ConectivityState.Off)
            .Ignore(ConectivityTrigger.NetworkError)
            .Ignore(ConectivityTrigger.OnNetworkTypeChanged);

            this._TriggerWithParameters = new ReadOnlyDictionary<ConectivityTrigger, StateMachine<ConectivityState, ConectivityTrigger>.TriggerWithParameters<Dictionary<string, object?>?>>(
                new Dictionary<ConectivityTrigger, StateMachine<ConectivityState, ConectivityTrigger>.TriggerWithParameters<Dictionary<string, object?>?>>
            {

                  {  ConectivityTrigger.NetworkError,
                    this.StateMachine.SetTriggerParameters<Dictionary<string, object?>?>(
                        ConectivityTrigger.NetworkError
                    )}

            });

            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
            this.StateMachine.Fire(GetConectivityTrigger());


        }

        ~ConectivityStateMachine()
        {
            Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
        }


        void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            this.StateMachine.Fire(GetConectivityTrigger(e.NetworkAccess));
        }

        private ConectivityTrigger GetConectivityTrigger(NetworkAccess? accessType = null)
        {
            accessType ??= Connectivity.Current.NetworkAccess;
            return this.NetworkAccessConectivity[(NetworkAccess)accessType];
        }

        internal static void Init()
        {

            Instance.StateMachine.OnTransitionCompleted(t =>
            {
                if (NavigationStateMachine.CurrentPage is IConectivityStatePage statePage)
                {
                    switch (t.Destination)
                    {
                        case ConectivityState.On:
                            statePage.OnConnectivityOn();
                            break;
                        case ConectivityState.Off:
                            statePage.OnConnectivityOff();
                            break;
                        case ConectivityState.Error:
                           statePage.OnConnectivityError();
                            break;
                    }
                }

                switch (t.Destination)
                {
                    case ConectivityState.Off:
                        NavigationStateMachine.FireIfYouCan(ON_DISCONECTED_FROM_INTERNET);
                        break;
                    case ConectivityState.Error:
                        NavigationStateMachine.FireIfYouCan(ON_NETWORK_ERROR);
                        break;
                    default:
                        NavigationStateMachine.FireIfYouCan(ON_CONNECTED_TO_INTERNET);
                        break;
                }
            });
        }

        public static void Fire(ConectivityTrigger trigger)
        {
            Instance.StateMachine.Fire(trigger);
        }
        public static ConectivityStateMachine Fire(
           ConectivityTrigger trigger,
           Dictionary<string, object?>? parameters
       )
        {
            var instance = Instance;
            var _trigger =
                instance._TriggerWithParameters[trigger]
                ?? throw new InvalidOperationException("Trigger is not defined for: " + trigger);
            instance.StateMachine.Fire(_trigger, parameters);
            return instance;
        }

        public static bool FireIfYouCan(ConectivityTrigger trigger)
        {
            if (Instance.StateMachine.CanFire(
                trigger
                    ))
            {
                Instance.StateMachine.Fire(
                trigger
                    );
                return true;
            }
            return false;
        }

    }
}
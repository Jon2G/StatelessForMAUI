using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StatelessForMAUI.StateMachine;

namespace StatelessForMAUI.Pages
{
    public abstract class StatelessNavigationContentPage : ContentPage, IAppLifeStatePage, IConectivityStatePage, INavigationEventsPage
    {
        protected StatelessNavigationContentPage()
        {

        }

        protected override bool OnBackButtonPressed()
        {
            return base.OnBackButtonPressed();
        }

        public virtual void OnBackground()
        {

        }

        public virtual void OnResume()
        {

        }

        public virtual void OnConnectivityOff()
        {

        }

        public virtual void OnConnectivityOn()
        {

        }

        public virtual void OnConnectivityError()
        {

        }

        public virtual void OnNavigatedAway(string? to)
        {

        }

        public virtual void OnNavigatedTo(string? from)
        {

        }
    }
}


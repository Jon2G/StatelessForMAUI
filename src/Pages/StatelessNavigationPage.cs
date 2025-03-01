using StatelessForMAUI.StateMachine;

namespace StatelessForMAUI.Pages
{
    internal class StatelessNavigationPage : NavigationPage, IAppLifeStatePage, IConectivityStatePage, INavigationEventsPage
    {
        internal StatelessNavigationPage(Page page) : base(page)
        {
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior()
            {
                Command = new Command(OnNavBarBackButtonPressed)
            });
        }
        private void OnNavBarBackButtonPressed(object obj)
        {
            throw new NotImplementedException();
        }

        protected override bool OnBackButtonPressed()
        {
            NavigationStateMachine.GoBack();
            return true;
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

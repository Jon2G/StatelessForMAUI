namespace StatelessForMAUI.Pages
{
    public abstract class StatelessNavigationContentPage : ContentPage, IAppLifeStatePage, IConectivityStatePage, INavigationEventsPage
    {
        protected override bool OnBackButtonPressed()
        {
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

using System.Reflection;
using StatelessForMAUI.StateMachine;

namespace StatelessForMAUI.Pages
{
    internal static class StatelessNavigationPageExtensions
    {
        internal static Page? UnBoxStatelessNavigationPage(this Page? from)
        {
            if(from is StatelessNavigationPage navigationPage)
            {
                return navigationPage.UnBoxStatelessNavigationPage();
            }
            return from;
        }
        internal static Page? UnBoxStatelessNavigationPage(this StatelessNavigationPage? from)
        {
            return from?.CurrentPage;
        }
    }
    internal class StatelessNavigationPage : NavigationPage, IAppLifeStatePage, IConectivityStatePage, INavigationEventsPage
    {
        internal StatelessNavigationPage(Page page) : base(page)
        {
            //Copy all bindale properties 
            //TODO: Copy only relevant props
            foreach (PropertyInfo propertyInfo in typeof(NavigationPage).GetProperties(BindingFlags.Static))
            {
                Console.WriteLine($"Property {propertyInfo.Name} copied");
                propertyInfo.SetValue(this,propertyInfo.GetValue(page));
            }
            //NavigationPage.SetHasNavigationBar(page,false);
            
            
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior()
            {
                Command = new Command(OnNavBarBackButtonPressed)
            });
            Shell.SetBackButtonBehavior(page, new BackButtonBehavior()
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
            if(CurrentPage is IAppLifeStatePage page)
            {
                page.OnBackground();
            }
        }

        public virtual void OnResume()
        {
            if(CurrentPage is IAppLifeStatePage page)
            {
                page.OnResume();
            }
        }

        public virtual void OnConnectivityOff()
        {
            if(CurrentPage is IConectivityStatePage page)
            {
                page.OnConnectivityOff();
            }
        }

        public virtual void OnConnectivityOn()
        {
            if(CurrentPage is IConectivityStatePage page)
            {
                page.OnConnectivityOn();
            }
        }

        public virtual void OnConnectivityError()
        {
            if(CurrentPage is IConectivityStatePage page)
            {
                page.OnConnectivityError();
            }
        }

    

        public virtual void OnNavigatedAway(Page? to)
        {
            if(CurrentPage is INavigationEventsPage page)
            {
                page.OnNavigatedAway(to?.UnBoxStatelessNavigationPage());
            }
        }

        public virtual void OnNavigatedTo(Page? from)
        {
            if(CurrentPage is INavigationEventsPage page)
            {
                page.OnNavigatedTo(from?.UnBoxStatelessNavigationPage());
            }
        }
    }
}

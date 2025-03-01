using StatelessForMAUI.StateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatelessForMAUI.Pages
{
    internal class StatelessShell : Shell
    {
        public StatelessShell(Page root) : base()
        {
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior()
            {
                Command = new Command(OnNavBarBackButtonPressed)
            });
            this.CurrentItem = new ShellContent()
            {
                Title = "Root",
                Route = "root",
                ContentTemplate = new DataTemplate(() => root)
            };
        }

        private void OnNavBarBackButtonPressed(object obj)
        {
            NavigationStateMachine.GoBack();
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
        }
        protected override bool OnBackButtonPressed()
        {
            return true;
        }
    }
}

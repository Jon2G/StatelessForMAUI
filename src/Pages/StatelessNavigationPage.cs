using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatelessForMAUI.Pages
{
    internal class StatelessNavigationPage(Page root) : NavigationPage(root)
    {
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

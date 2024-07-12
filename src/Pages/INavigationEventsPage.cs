using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatelessForMAUI.Pages
{
    public interface INavigationEventsPage
    {
       void OnNavigatedAway(string? to);
        void OnNavigatedTo(string? from);
    }
}

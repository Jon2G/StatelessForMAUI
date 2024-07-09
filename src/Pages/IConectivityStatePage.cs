using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatelessForMAUI.Pages
{
    public interface IConectivityStatePage
    {
        void OnConnectivityOff();
        void OnConnectivityOn();
        void OnConnectivityError();
    }
}

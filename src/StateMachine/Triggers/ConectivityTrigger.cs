using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatelessForMAUI.StateMachine.Triggers
{
    public enum ConectivityTrigger
    {
        OnDisconnected,
        OnConnected,
        OnNetworkTypeChanged,
        NetworkError
    }
}

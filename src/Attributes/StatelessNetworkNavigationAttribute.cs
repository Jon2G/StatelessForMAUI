using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatelessForMAUI.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class StatelessNetworkNavigationAttribute:Attribute
    {
        public StatelessNetworkNavigationAttribute(
            Type? onDisconectedFromInternet,
            Type? onConnectedToInternet,
            Type? onNetworkError
            )
        {
            
        }
    }
}

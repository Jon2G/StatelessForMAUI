using StatelessForMAUI.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatelessForMAUI.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class StatelessNavigationAttribute : Attribute
    {
        internal readonly Type[]? allowedTransitions = null;
        internal readonly Type[]? ignoredTransitions = null;
        internal readonly bool canGoBack;
        internal readonly Type? goBackTarget = null;
        internal readonly bool useNetworkTriggers;
        internal readonly Type? onConnectedToInternet = null;
        internal readonly bool permitReentry = false;
        internal readonly bool selfIgnore = true;
        internal readonly bool isRoot = false;
        public StatelessNavigationAttribute(

            Type[]? allowedTransitions = null,
              Type[]? ignoredTransitions = null,
            bool canGoBack = true,
            Type? GoBackTarget = null,
            bool useNetworkTriggers = true,
           Type? onConnectedToInternet = null,
           bool permitReentry = false,
            bool selfIgnore = true,
            bool isRoot = false
            )
        {
            this.allowedTransitions = allowedTransitions;
            this.ignoredTransitions = ignoredTransitions;
            this.canGoBack = canGoBack;
            this.goBackTarget = GoBackTarget;
            this.useNetworkTriggers = useNetworkTriggers;
            this.onConnectedToInternet = onConnectedToInternet;
            this.permitReentry = permitReentry;
            this.selfIgnore = selfIgnore;
            this.isRoot = isRoot;
        }



    }
}

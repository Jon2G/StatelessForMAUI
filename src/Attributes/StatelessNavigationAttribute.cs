using StatelessForMAUI.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatelessForMAUI.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class StatelessNavigationAttribute(

        Type[]? allowedTransitions = null,
          Type[]? ignoredTransitions = null,
        bool canGoBack = true,
        Type? GoBackTarget = null,
        bool useNetworkTriggers = true,
       Type? onConnectedToInternet = null,
       bool permitReentry = false,
        bool selfIgnore = true,
        bool isRoot = false,
        bool isModal = false
            ) : Attribute
    {
        internal readonly Type[]? allowedTransitions = allowedTransitions;
        internal readonly Type[]? ignoredTransitions = ignoredTransitions;
        internal readonly bool canGoBack = canGoBack;
        internal readonly Type? goBackTarget = GoBackTarget;
        internal readonly bool useNetworkTriggers = useNetworkTriggers;
        internal readonly Type? onConnectedToInternet = onConnectedToInternet;
        internal readonly bool permitReentry = permitReentry;
        internal readonly bool selfIgnore = selfIgnore;
        internal readonly bool isRoot = isRoot;
        internal readonly bool isModal = isModal;
    }
}

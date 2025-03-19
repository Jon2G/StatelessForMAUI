namespace StatelessForMAUI.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class SplashPageAttribute(

    Type[]? allowedTransitions = null,
    Type[]? ignoredTransitions = null,
    bool useNetworkTriggers = true,
    Type? onConnectedToInternet = null
) : StatelessNavigationAttribute(allowedTransitions, ignoredTransitions, canGoBack: false, GoBackTarget: null, useNetworkTriggers, onConnectedToInternet, permitReentry: false, selfIgnore: true, isRoot: true, isModal: false)
{
}
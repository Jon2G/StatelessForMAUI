using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using StatelessForMAUI.StateMachine;
using System.Reflection;

namespace StatelessForMAUI
{

    public static class StatelessForMAUI
    {
        public static MauiAppBuilder UseStatelessForMauiApp(this MauiAppBuilder builder)
        {
           
            builder.Services.AddSingleton<ConectivityStateMachine>((s)=> StatelessForMAUIApp.ConectivityStateMachine);
            builder.Services.AddSingleton<NavigationStateMachine>((s)=> StatelessForMAUIApp.NavigationStateMachine);
            builder.Services.AddSingleton<AppLifeStateMachine>((s) => StatelessForMAUIApp.AppLifeStateMachine);

            return builder;
        }

    }
}

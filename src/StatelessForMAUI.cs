using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using StatelessForMAUI.StateMachine;
using System.Reflection;

namespace StatelessForMAUI
{

    public static class StatelessForMaui
    {
        public static MauiAppBuilder UseStatelessForMauiApp(this MauiAppBuilder builder)
        {
           
            builder.Services.AddSingleton<ConnectivityStateMachine>((s)=> StatelessForMauiApp.ConnectivityStateMachine ?? throw new NullReferenceException("ConnectivityStateMachine has not been activated yet."));
            builder.Services.AddSingleton<NavigationStateMachine>((s)=> StatelessForMauiApp.NavigationStateMachine ?? throw new NullReferenceException("NavigationStateMachine has not been activated yet."));
            builder.Services.AddSingleton<AppLifeStateMachine>((s) => StatelessForMauiApp.AppLifeStateMachine ?? throw new NullReferenceException("AppLifeStateMachine has not been activated yet."));

            return builder;
        }

    }
}

using Microsoft.Maui.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Views;
using Microsoft.Maui.Platform;

namespace StatelessForMAUI.Platforms.Droid
{
    internal static class PageHandler
    {
        public static void Handle(IPageHandler handler, IContentView view)
        {
            Android.Views.ViewGroup platformView = handler.PlatformView;
            platformView.KeyPress += PlatformView_KeyPress;

            IViewParent? parent=platformView.Parent;

            object? containerView = handler.ContainerView;
        }

        private static void PlatformView_KeyPress(object? sender, Android.Views.View.KeyEventArgs e)
        {
            e.Handled=true;
        }
    }
}

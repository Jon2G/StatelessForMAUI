using Android.Window;
using AndroidX.Activity;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Runtime;

namespace StatelessForMAUI.Droid
{
    public class OnBackInvokedCallback() : OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed()
        {
            Enabled = true;
          
        }
    }
}

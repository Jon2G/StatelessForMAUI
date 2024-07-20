using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.Activity;
using StatelessForMAUI.StateMachine;

namespace StatelessForMAUI.Platforms.Android
{
    public class BackPress(Activity activity) : OnBackPressedCallback(true)
    {
        private readonly Activity activity = activity;
        private static long backPressed;

        public override void HandleOnBackPressed()
        {
            OnBackPressed(this.activity);
        }

        public static void OnBackPressed(Activity activity)
        {
            var previousState =StateMachine.NavigationStateMachine.Instance.StateMachine.State;
            NavigationStateMachine.Fire(NavigationStateMachine.GO_BACK);
            var currentState = NavigationStateMachine.Instance.StateMachine.State;
            if (previousState == currentState)
            {
                const int delay = 2000;
                if (backPressed + delay > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    activity.FinishAndRemoveTask();
                    Process.KillProcess(Process.MyPid());
                }
                else
                {
                    backPressed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }
            }

        }
    }
}

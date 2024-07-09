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

namespace StatelessForMAUI
{
    public class BackPress : OnBackPressedCallback
    {
        private readonly Activity activity;
        private static long backPressed;

        public BackPress(Activity activity)
            : base(true)
        {
            this.activity = activity;
        }

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
                    //App.AlertConfirmation("Question?", "Would you like to play a game", "Sí", "No!")
                    //    .ContinueWith(t =>
                    //    {
                    //        Console.WriteLine(t.Result);
                    //    });
                    //App.ToastSnack("Close");
                    //Toast.MakeText(activity, "Close", ToastLength.Long)?.Show();
                    backPressed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }
            }

        }
    }
}

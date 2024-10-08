﻿using Android.App;
using Android.Content.PM;
using Android.OS;
using StatelessForMAUI.Platforms.Android;

namespace SampleApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        OnBackPressedDispatcher.AddCallback(new BackPress(this));
    }
    [Obsolete]
    public override void OnBackPressed()
    {
        //base.OnBackPressed(); DONT FORGET TO REMOVE BASE CALL
        BackPress.OnBackPressed(this);
    }
}

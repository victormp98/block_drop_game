using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace BlockDrop;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ScreenOrientation = ScreenOrientation.Portrait, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        EnterFullScreen();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus) EnterFullScreen();
    }

    private void EnterFullScreen()
    {
        if (Window is null) return;
        var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
        if (controller is null) return;
        controller.Hide(WindowInsetsCompat.Type.SystemBars());
        controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
    }
}

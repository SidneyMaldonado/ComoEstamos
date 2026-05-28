using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace ComoEstamos
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Window == null) return;

            // Permite que o conteúdo se estenda atrás da status bar
            WindowCompat.SetDecorFitsSystemWindows(Window, false);

#pragma warning disable CA1422
            // Torna a status bar transparente para o conteúdo aparecer atrás
            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#1A2E6E"));
#pragma warning restore CA1422

            // Ícones da status bar em branco (para fundo escuro)
            var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
            controller.AppearanceLightStatusBars = false;
        }
    }
}

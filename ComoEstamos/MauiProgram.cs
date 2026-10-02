using ComoEstamos.Core;
using ComoEstamos.Data;
using ComoEstamos.Pages;
using ComoEstamos.Services;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace ComoEstamos
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            var caminhoBanco = Path.Combine(FileSystem.AppDataDirectory, "comoestamos.db3");
            builder.Services.AddComoEstamosData(caminhoBanco);
            builder.Services.AddComoEstamosCore();

            builder.Services.AddSingleton<IUsuarioAtual, UsuarioAtual>();

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ContasPage>();
            builder.Services.AddTransient<ContaPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            app.Services.AplicarMigrations();
            return app;
        }
    }
}

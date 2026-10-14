using ComoEstamos.Core;
using ComoEstamos.Data;
using ComoEstamos.Pages.Carteiras;
using ComoEstamos.Pages.Categorias;
using ComoEstamos.Pages.Contas;
using ComoEstamos.Pages.Credores;
using ComoEstamos.Pages.Dividas;
using ComoEstamos.Pages.Parcelas;
using ComoEstamos.Pages.Investimentos;
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

            builder.Services.AddTransient<MainPage>();

            // As 4 páginas de cada CRUD (Spec/07-Crud.md).
            builder.Services.AddTransient<ListarContasPage>();
            builder.Services.AddTransient<VisualizarContaPage>();
            builder.Services.AddTransient<IncluirContaPage>();
            builder.Services.AddTransient<AlterarContaPage>();
            builder.Services.AddTransient<AtualizarSaldosPage>();

            builder.Services.AddTransient<ListarCategoriasPage>();
            builder.Services.AddTransient<VisualizarCategoriaPage>();
            builder.Services.AddTransient<IncluirCategoriaPage>();
            builder.Services.AddTransient<AlterarCategoriaPage>();

            builder.Services.AddTransient<ListarCredoresPage>();
            builder.Services.AddTransient<VisualizarCredorPage>();
            builder.Services.AddTransient<IncluirCredorPage>();
            builder.Services.AddTransient<AlterarCredorPage>();

            builder.Services.AddTransient<ListarCarteirasPage>();
            builder.Services.AddTransient<VisualizarCarteiraPage>();
            builder.Services.AddTransient<IncluirCarteiraPage>();
            builder.Services.AddTransient<AlterarCarteiraPage>();

            builder.Services.AddTransient<ListarInvestimentosPage>();
            builder.Services.AddTransient<VisualizarInvestimentoPage>();
            builder.Services.AddTransient<IncluirInvestimentoPage>();
            builder.Services.AddTransient<AlterarInvestimentoPage>();

            // Dívidas: listar (consulta com filtros), visualizar, incluir e alterar; excluir é pela visualizar.
            builder.Services.AddTransient<ListarDividasPage>();
            builder.Services.AddTransient<VisualizarDividaPage>();
            builder.Services.AddTransient<IncluirDividaPage>();
            builder.Services.AddTransient<AlterarDividaPage>();
            builder.Services.AddTransient<VisualizarParcelaPage>();
            builder.Services.AddTransient<IncluirParcelaPage>();
            builder.Services.AddTransient<AlterarParcelaPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            app.Services.AplicarMigrations();
            return app;
        }
    }
}

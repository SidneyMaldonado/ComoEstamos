using ComoEstamos.Pages.Carteiras;
using ComoEstamos.Pages.Categorias;
using ComoEstamos.Pages.Contas;
using ComoEstamos.Pages.Credores;
using ComoEstamos.Pages.Dividas;
using ComoEstamos.Pages.Parcelas;
using ComoEstamos.Pages.Receitas;
using ComoEstamos.Pages.Investimentos;

namespace ComoEstamos
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Páginas fora do menu: incluir, alterar e visualizar de cada CRUD (Spec/07-Crud.md).
            Routing.RegisterRoute(IncluirContaPage.Rota, typeof(IncluirContaPage));
            Routing.RegisterRoute(AlterarContaPage.Rota, typeof(AlterarContaPage));
            Routing.RegisterRoute(VisualizarContaPage.Rota, typeof(VisualizarContaPage));
            Routing.RegisterRoute(AtualizarSaldosPage.Rota, typeof(AtualizarSaldosPage));

            Routing.RegisterRoute(IncluirCategoriaPage.Rota, typeof(IncluirCategoriaPage));
            Routing.RegisterRoute(AlterarCategoriaPage.Rota, typeof(AlterarCategoriaPage));
            Routing.RegisterRoute(VisualizarCategoriaPage.Rota, typeof(VisualizarCategoriaPage));

            Routing.RegisterRoute(IncluirCredorPage.Rota, typeof(IncluirCredorPage));
            Routing.RegisterRoute(AlterarCredorPage.Rota, typeof(AlterarCredorPage));
            Routing.RegisterRoute(VisualizarCredorPage.Rota, typeof(VisualizarCredorPage));

            Routing.RegisterRoute(IncluirCarteiraPage.Rota, typeof(IncluirCarteiraPage));
            Routing.RegisterRoute(AlterarCarteiraPage.Rota, typeof(AlterarCarteiraPage));
            Routing.RegisterRoute(VisualizarCarteiraPage.Rota, typeof(VisualizarCarteiraPage));

            Routing.RegisterRoute(IncluirInvestimentoPage.Rota, typeof(IncluirInvestimentoPage));
            Routing.RegisterRoute(AlterarInvestimentoPage.Rota, typeof(AlterarInvestimentoPage));
            Routing.RegisterRoute(VisualizarInvestimentoPage.Rota, typeof(VisualizarInvestimentoPage));

            Routing.RegisterRoute(IncluirDividaPage.Rota, typeof(IncluirDividaPage));
            Routing.RegisterRoute(AlterarDividaPage.Rota, typeof(AlterarDividaPage));
            Routing.RegisterRoute(VisualizarDividaPage.Rota, typeof(VisualizarDividaPage));

            Routing.RegisterRoute(IncluirReceitaPage.Rota, typeof(IncluirReceitaPage));
            Routing.RegisterRoute(AlterarReceitaPage.Rota, typeof(AlterarReceitaPage));
            Routing.RegisterRoute(VisualizarReceitaPage.Rota, typeof(VisualizarReceitaPage));

            Routing.RegisterRoute(VisualizarParcelaPage.Rota, typeof(VisualizarParcelaPage));
            Routing.RegisterRoute(IncluirParcelaPage.Rota, typeof(IncluirParcelaPage));
            Routing.RegisterRoute(AlterarParcelaPage.Rota, typeof(AlterarParcelaPage));
        }
    }
}

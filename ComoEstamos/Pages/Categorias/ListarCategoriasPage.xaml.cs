using ComoEstamos.Controls;
using ComoEstamos.Core.Services;

namespace ComoEstamos.Pages.Categorias
{
    /// <summary>Lista das categorias ativas. Tocar numa linha abre a visualização; o "+" abre a inclusão.</summary>
    public partial class ListarCategoriasPage : ContentPage
    {
        private readonly ICategoriaService categoriaService;

        public ListarCategoriasPage(ICategoriaService categoriaService)
        {
            this.categoriaService = categoriaService;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var categorias = await categoriaService.BuscarTodasAsync();
            Tabela.Linhas = LinhaTabela.Montar(categorias, c => c.Id, c => c.Nome);
        }

        private void Menu_Clicked(object? sender, EventArgs e) => Shell.Current.FlyoutIsPresented = true;

        private async void Incluir_Tapped(object? sender, TappedEventArgs e) =>
            await Shell.Current.GoToAsync(IncluirCategoriaPage.Rota);

        private async void Tabela_LinhaTocada(object? sender, LinhaTabela linha) =>
            await Shell.Current.GoToAsync($"{VisualizarCategoriaPage.Rota}?id={linha.Id}");
    }
}

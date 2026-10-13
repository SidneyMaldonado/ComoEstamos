using ComoEstamos.Controls;
using ComoEstamos.Core.Services;

namespace ComoEstamos.Pages.Credores
{
    /// <summary>Lista dos credores ativos. Tocar numa linha abre a visualização; o "+" abre a inclusão.</summary>
    public partial class ListarCredoresPage : ContentPage
    {
        private readonly ICredorService credorService;

        public ListarCredoresPage(ICredorService credorService)
        {
            this.credorService = credorService;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var credores = await credorService.BuscarTodosAsync();
            Tabela.Linhas = LinhaTabela.Montar(credores, c => c.Id, c => c.Nome);
        }

        private void Menu_Clicked(object? sender, EventArgs e) => Shell.Current.FlyoutIsPresented = true;

        private async void Incluir_Tapped(object? sender, TappedEventArgs e) =>
            await Shell.Current.GoToAsync(IncluirCredorPage.Rota);

        private async void Tabela_LinhaTocada(object? sender, LinhaTabela linha) =>
            await Shell.Current.GoToAsync($"{VisualizarCredorPage.Rota}?id={linha.Id}");
    }
}

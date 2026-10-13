using ComoEstamos.Controls;
using ComoEstamos.Core.Services;

namespace ComoEstamos.Pages.Carteiras
{
    /// <summary>Lista das carteiras ativas. Tocar numa linha abre a visualização; o "+" abre a inclusão.</summary>
    public partial class ListarCarteirasPage : ContentPage
    {
        private readonly ICarteiraService carteiraService;

        public ListarCarteirasPage(ICarteiraService carteiraService)
        {
            this.carteiraService = carteiraService;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var carteiras = await carteiraService.BuscarTodasAsync();
            Tabela.Linhas = LinhaTabela.Montar(carteiras, c => c.Id, c => c.Nome);
        }

        private void Menu_Clicked(object? sender, EventArgs e) => Shell.Current.FlyoutIsPresented = true;

        private async void Incluir_Tapped(object? sender, TappedEventArgs e) =>
            await Shell.Current.GoToAsync(IncluirCarteiraPage.Rota);

        private async void Tabela_LinhaTocada(object? sender, LinhaTabela linha) =>
            await Shell.Current.GoToAsync($"{VisualizarCarteiraPage.Rota}?id={linha.Id}");
    }
}

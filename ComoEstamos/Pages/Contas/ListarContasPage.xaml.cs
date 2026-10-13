using ComoEstamos.Controls;
using ComoEstamos.Core.Services;

namespace ComoEstamos.Pages.Contas
{
    /// <summary>Lista das contas ativas. Tocar numa linha abre a visualização; o "+" abre a inclusão.</summary>
    public partial class ListarContasPage : ContentPage
    {
        private readonly IContaService contaService;

        public ListarContasPage(IContaService contaService)
        {
            this.contaService = contaService;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var contas = await contaService.BuscarTodasAsync();
            Tabela.Linhas = LinhaTabela.Montar(contas, c => c.Id, c => c.Nome, valor: c => c.Saldo);
        }

        private void Menu_Clicked(object? sender, EventArgs e) => Shell.Current.FlyoutIsPresented = true;

        private async void Incluir_Tapped(object? sender, TappedEventArgs e) =>
            await Shell.Current.GoToAsync(IncluirContaPage.Rota);

        private async void Tabela_LinhaTocada(object? sender, LinhaTabela linha) =>
            await Shell.Current.GoToAsync($"{VisualizarContaPage.Rota}?id={linha.Id}");
    }
}

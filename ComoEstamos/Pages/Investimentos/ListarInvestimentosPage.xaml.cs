using ComoEstamos.Controls;
using ComoEstamos.Core.Services;

namespace ComoEstamos.Pages.Investimentos
{
    /// <summary>Lista dos investimentos ativos. Tocar numa linha abre a visualização; o "+" abre a inclusão.</summary>
    public partial class ListarInvestimentosPage : ContentPage
    {
        private readonly IInvestimentoService investimentoService;

        public ListarInvestimentosPage(IInvestimentoService investimentoService)
        {
            this.investimentoService = investimentoService;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var investimentos = await investimentoService.BuscarTodosAsync();
            Tabela.Linhas = LinhaTabela.Montar(investimentos, i => i.Id, i => i.Nome, i => i.Carteira?.Nome, i => i.Quantidade * i.Cotacao);
        }

        private void Menu_Clicked(object? sender, EventArgs e) => Shell.Current.FlyoutIsPresented = true;

        private async void Incluir_Tapped(object? sender, TappedEventArgs e) =>
            await Shell.Current.GoToAsync(IncluirInvestimentoPage.Rota);

        private async void Tabela_LinhaTocada(object? sender, LinhaTabela linha) =>
            await Shell.Current.GoToAsync($"{VisualizarInvestimentoPage.Rota}?id={linha.Id}");
    }
}

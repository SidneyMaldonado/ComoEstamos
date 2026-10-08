using ComoEstamos.Controls;
using ComoEstamos.Core.Services;

namespace ComoEstamos
{
    public partial class MainPage : ContentPage
    {
        private readonly IContaService contaService;

        public MainPage(IContaService contaService)
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
    }
}

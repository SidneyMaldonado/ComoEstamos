using ComoEstamos.Compartilhamento;
using ComoEstamos.Controls;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;
using ComoEstamos.Pages.Contas;

namespace ComoEstamos
{
    public partial class MainPage : ContentPage
    {
        private readonly IContaService contaService;

        private List<Conta> contas = [];

        public MainPage(IContaService contaService)
        {
            this.contaService = contaService;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            contas = await contaService.BuscarTodasAsync();
            Tabela.Linhas = LinhaTabela.Montar(contas, c => c.Id, c => c.Nome, valor: c => c.Saldo);
            Tabela.Total = contas.Sum(c => c.Saldo);
        }

        private async void AtualizarSaldos_Clicked(object? sender, EventArgs e) =>
            await Shell.Current.GoToAsync(AtualizarSaldosPage.Rota);

        /// <summary>Gera o PNG da tabela (contas, saldos e total) e abre o compartilhamento do sistema.</summary>
        private async void Compartilhar_Tapped(object? sender, TappedEventArgs e)
        {
            if (contas.Count == 0)
            {
                await DisplayAlertAsync("Compartilhar", "Nenhuma conta cadastrada.", "OK");
                return;
            }

            BotaoCompartilhar.IsEnabled = false;
            try
            {
                var hoje = DateTime.Now;
                var png = ImagemSaldos.GerarPng([.. contas.Select(c => (c.Nome, c.Saldo))], contas.Sum(c => c.Saldo), hoje);
                var caminho = Path.Combine(FileSystem.CacheDirectory, $"como-estamos-{hoje:yyyy-MM-dd}.png");
                await File.WriteAllBytesAsync(caminho, png);

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Como Estamos",
                    File = new ShareFile(caminho, "image/png"),
                });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Não foi possível compartilhar", ex.Message, "OK");
            }
            finally
            {
                BotaoCompartilhar.IsEnabled = true;
            }
        }

        private void Menu_Clicked(object? sender, EventArgs e) => Shell.Current.FlyoutIsPresented = true;

        private async void ArrastarParaCima_Swiped(object? sender, SwipedEventArgs e) => await AbrirResumoAsync();

        private async void Resumo_Tapped(object? sender, TappedEventArgs e) => await AbrirResumoAsync();

        /// <summary>Abre os investimentos como o item "Investimentos" do menu.</summary>
        private async void ArrastarParaBaixo_Swiped(object? sender, SwipedEventArgs e) =>
            await Shell.Current.GoToAsync("//investimentos");

        /// <summary>Abre o resumo do mês como o item "Resumo do Mês" do menu (rota absoluta do Shell).</summary>
        private static Task AbrirResumoAsync() => Shell.Current.GoToAsync("//resumo");
    }
}

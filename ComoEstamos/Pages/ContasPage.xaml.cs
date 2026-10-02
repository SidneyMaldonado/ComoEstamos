using ComoEstamos.Controls;
using ComoEstamos.Data.Repository;
using ComoEstamos.Services;

namespace ComoEstamos.Pages
{
    /// <summary>Lista das contas ativas do usuário. Tocar numa linha abre a alteração; o "+" abre a inclusão.</summary>
    public partial class ContasPage : ContentPage
    {
        private readonly IContaRepository contaRepository;
        private readonly IUsuarioAtual usuarioAtual;

        public ContasPage(IContaRepository contaRepository, IUsuarioAtual usuarioAtual)
        {
            this.contaRepository = contaRepository;
            this.usuarioAtual = usuarioAtual;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var contas = await contaRepository.ListarPorUsuarioAsync(await usuarioAtual.ObterIdAsync());
            Tabela.Contas = ContaLinha.Montar(contas);
        }

        private void Menu_Clicked(object? sender, EventArgs e) => Shell.Current.FlyoutIsPresented = true;

        private async void Incluir_Tapped(object? sender, TappedEventArgs e) =>
            await Shell.Current.GoToAsync(ContaPage.Rota);

        private async void Tabela_ContaTocada(object? sender, ContaLinha linha) =>
            await Shell.Current.GoToAsync($"{ContaPage.Rota}?id={linha.Id}");
    }
}

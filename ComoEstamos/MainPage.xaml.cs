using ComoEstamos.Controls;
using ComoEstamos.Data.Repository;
using ComoEstamos.Services;

namespace ComoEstamos
{
    public partial class MainPage : ContentPage
    {
        private readonly IContaRepository contaRepository;
        private readonly IUsuarioAtual usuarioAtual;

        public MainPage(IContaRepository contaRepository, IUsuarioAtual usuarioAtual)
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
    }
}

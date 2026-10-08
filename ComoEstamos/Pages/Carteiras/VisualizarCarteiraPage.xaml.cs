using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Carteiras
{
    /// <summary>
    /// Visualização da carteira, só de leitura. O lápis abre a alteração; a lixeira pede
    /// confirmação e exclui (desativa).
    /// </summary>
    public partial class VisualizarCarteiraPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "visualizar-carteira";

        private readonly ICarteiraService carteiraService;

        private int idCarteira;
        private Carteira? carteira;

        public VisualizarCarteiraPage(ICarteiraService carteiraService)
        {
            this.carteiraService = carteiraService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idCarteira = Formulario.LerId(query);

        /// <summary>Recarrega sempre, para refletir o que foi alterado na página Alterar.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            carteira = await carteiraService.BuscarPorIdAsync(idCarteira);
            if (carteira is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Carteira não encontrada", "A carteira não existe mais ou foi excluída.");
                return;
            }

            ValorNome.Text = carteira.Nome;
            ValorDataCriacao.Text = FormatoBr.DataHora(carteira.DataCriacao);
            ValorDataAlteracao.Text = FormatoBr.DataHora(carteira.DataAlteracao);
        }

        private async void Alterar_Tapped(object? sender, TappedEventArgs e)
        {
            if (carteira is not null)
                await Shell.Current.GoToAsync($"{AlterarCarteiraPage.Rota}?id={carteira.Id}");
        }

        private async void Excluir_Tapped(object? sender, TappedEventArgs e)
        {
            if (carteira is null)
                return;

            var id = carteira.Id;
            await Formulario.ConfirmarEExcluirAsync(this,
                "Excluir carteira", $"Confirma a exclusão da carteira \"{carteira.Nome}\"?",
                () => carteiraService.ExcluirAsync(id));
        }

        private async void Voltar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();
    }
}

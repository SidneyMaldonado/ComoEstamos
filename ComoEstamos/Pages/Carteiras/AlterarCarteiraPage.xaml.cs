using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Carteiras
{
    /// <summary>Alteração de carteira. Carrega a carteira do <c>id</c> recebido; Salvar valida, grava e volta.</summary>
    public partial class AlterarCarteiraPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "alterar-carteira";

        private readonly ICarteiraService carteiraService;

        private int idCarteira;
        private bool carregada;

        public AlterarCarteiraPage(ICarteiraService carteiraService)
        {
            this.carteiraService = carteiraService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idCarteira = Formulario.LerId(query);

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (carregada)
                return;
            carregada = true;

            var carteira = await carteiraService.BuscarPorIdAsync(idCarteira);
            if (carteira is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Carteira não encontrada", "A carteira não existe mais ou foi excluída.");
                return;
            }

            CampoNome.Text = carteira.Nome;
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroNome);
            var carteira = new Carteira
            {
                Id = idCarteira,
                Nome = CampoNome.Text ?? string.Empty,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => carteiraService.AlterarAsync(carteira), Erros()))
                await Formulario.VoltarAsync();
        }

        private async void Cancelar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();

        /// <summary>Label de erro de cada propriedade validada pelo caso de uso.</summary>
        private Dictionary<string, Label> Erros() => new()
        {
            [nameof(Carteira.Nome)] = ErroNome,
        };
    }
}

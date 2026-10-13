using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Carteiras
{
    /// <summary>Inclusão de carteira. Salvar valida, inclui e volta para a lista.</summary>
    public partial class IncluirCarteiraPage : ContentPage
    {
        public const string Rota = "incluir-carteira";

        private readonly ICarteiraService carteiraService;

        public IncluirCarteiraPage(ICarteiraService carteiraService)
        {
            this.carteiraService = carteiraService;
            InitializeComponent();
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroNome);
            var carteira = new Carteira
            {
                Nome = CampoNome.Text ?? string.Empty,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => carteiraService.IncluirAsync(carteira), Erros()))
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

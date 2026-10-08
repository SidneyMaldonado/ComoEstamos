using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Credores
{
    /// <summary>Inclusão de credor. Salvar valida, inclui e volta para a lista.</summary>
    public partial class IncluirCredorPage : ContentPage
    {
        public const string Rota = "incluir-credor";

        private readonly ICredorService credorService;

        public IncluirCredorPage(ICredorService credorService)
        {
            this.credorService = credorService;
            InitializeComponent();
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroNome, ErroObservacoes);
            var credor = new Credor
            {
                Nome = CampoNome.Text ?? string.Empty,
                Observacoes = CampoObservacoes.Text,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => credorService.IncluirAsync(credor), Erros()))
                await Formulario.VoltarAsync();
        }

        private async void Cancelar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();

        /// <summary>Label de erro de cada propriedade validada pelo caso de uso.</summary>
        private Dictionary<string, Label> Erros() => new()
        {
            [nameof(Credor.Nome)] = ErroNome,
            [nameof(Credor.Observacoes)] = ErroObservacoes,
        };
    }
}

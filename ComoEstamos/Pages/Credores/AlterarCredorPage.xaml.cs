using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Credores
{
    /// <summary>Alteração de credor. Carrega o credor do <c>id</c> recebido; Salvar valida, grava e volta.</summary>
    public partial class AlterarCredorPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "alterar-credor";

        private readonly ICredorService credorService;

        private int idCredor;
        private bool carregada;

        public AlterarCredorPage(ICredorService credorService)
        {
            this.credorService = credorService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idCredor = Formulario.LerId(query);

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (carregada)
                return;
            carregada = true;

            var credor = await credorService.BuscarPorIdAsync(idCredor);
            if (credor is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Credor não encontrado", "O credor não existe mais ou foi excluído.");
                return;
            }

            CampoNome.Text = credor.Nome;
            CampoObservacoes.Text = credor.Observacoes;
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroNome, ErroObservacoes);
            var credor = new Credor
            {
                Id = idCredor,
                Nome = CampoNome.Text ?? string.Empty,
                Observacoes = CampoObservacoes.Text,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => credorService.AlterarAsync(credor), Erros()))
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

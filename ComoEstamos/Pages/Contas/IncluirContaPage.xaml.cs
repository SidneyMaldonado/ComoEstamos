using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Contas
{
    /// <summary>Inclusão de conta. Salvar valida, inclui e volta para a lista.</summary>
    public partial class IncluirContaPage : ContentPage
    {
        public const string Rota = "incluir-conta";

        private readonly IContaService contaService;

        public IncluirContaPage(IContaService contaService)
        {
            this.contaService = contaService;
            InitializeComponent();
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroNome, ErroSaldo);
            if (!Formulario.TentarLerDecimal(CampoSaldo, ErroSaldo, "o saldo", "1.250,00", out var saldo))
                return;

            var conta = new Conta
            {
                Nome = CampoNome.Text ?? string.Empty,
                Saldo = saldo,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => contaService.IncluirAsync(conta), Erros()))
                await Formulario.VoltarAsync();
        }

        private async void Cancelar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();

        /// <summary>Label de erro de cada propriedade validada pelo caso de uso.</summary>
        private Dictionary<string, Label> Erros() => new()
        {
            [nameof(Conta.Nome)] = ErroNome,
            [nameof(Conta.Saldo)] = ErroSaldo,
        };
    }
}

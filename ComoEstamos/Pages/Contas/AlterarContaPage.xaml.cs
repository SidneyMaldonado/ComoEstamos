using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Contas
{
    /// <summary>Alteração de conta. Carrega a conta do <c>id</c> recebido; Salvar valida, grava e volta.</summary>
    public partial class AlterarContaPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "alterar-conta";

        private readonly IContaService contaService;

        private int idConta;
        private bool carregada;

        public AlterarContaPage(IContaService contaService)
        {
            this.contaService = contaService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idConta = Formulario.LerId(query);

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (carregada)
                return;
            carregada = true;

            var conta = await contaService.BuscarPorIdAsync(idConta);
            if (conta is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Conta não encontrada", "A conta não existe mais ou foi excluída.");
                return;
            }

            CampoNome.Text = conta.Nome;
            CampoSaldo.Text = FormatoBr.NumeroFixo(conta.Saldo, 2);
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroNome, ErroSaldo);
            if (!Formulario.TentarLerDecimal(CampoSaldo, ErroSaldo, "o saldo", "1.250,00", out var saldo))
                return;

            var conta = new Conta
            {
                Id = idConta,
                Nome = CampoNome.Text ?? string.Empty,
                Saldo = saldo,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => contaService.AlterarAsync(conta), Erros()))
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

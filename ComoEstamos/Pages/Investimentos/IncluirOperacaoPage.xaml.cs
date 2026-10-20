using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Investimentos
{
    /// <summary>
    /// Registro de uma compra ou venda do investimento do <c>id</c> recebido (aberta pelo + da Visualizar
    /// Investimento). Vem com Compra, a data e a hora de agora. Salvar valida, grava e volta; a quantidade do
    /// investimento muda junto (a cotação não).
    /// </summary>
    public partial class IncluirOperacaoPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "incluir-operacao";

        private readonly IOperacaoService operacaoService;
        private readonly IInvestimentoService investimentoService;

        private int idInvestimento;
        private bool carregada;

        public IncluirOperacaoPage(IOperacaoService operacaoService, IInvestimentoService investimentoService)
        {
            this.operacaoService = operacaoService;
            this.investimentoService = investimentoService;
            InitializeComponent();
        }

        /// <summary>O <c>id</c> da rota é o do investimento.</summary>
        public void ApplyQueryAttributes(IDictionary<string, object> query) => idInvestimento = Formulario.LerId(query);

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (carregada)
                return;
            carregada = true;

            var investimento = await investimentoService.BuscarPorIdAsync(idInvestimento);
            if (investimento is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Investimento não encontrado", "O investimento não existe mais ou foi excluído.");
                return;
            }

            Aviso.Text = $"Operação de \"{investimento.Nome}\", com {FormatoBr.Numero(investimento.Quantidade, 6)} hoje. " +
                "A compra soma na quantidade e a venda subtrai; a cotação não muda.";
            var agora = DateTime.Now;
            CampoTipo.SelectedIndex = 0;
            CampoData.MaximumDate = agora.Date;
            CampoData.Date = agora.Date;
            CampoHora.Time = new TimeSpan(agora.Hour, agora.Minute, 0);
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroData, ErroQuantidade, ErroValorTotal);
            var quantidadeLida = Formulario.TentarLerDecimal(CampoQuantidade, ErroQuantidade, "a quantidade", "10,5", out var quantidade);
            var valorLido = Formulario.TentarLerDecimal(CampoValorTotal, ErroValorTotal, "o valor total", "325,00", out var valorTotal);
            if (!quantidadeLida || !valorLido)
                return;

            var operacao = new Operacao
            {
                IdInvestimento = idInvestimento,
                EhCompra = CampoTipo.SelectedIndex != 1,
                DataOperacao = (CampoData.Date ?? DateTime.Today).Date + (CampoHora.Time ?? TimeSpan.Zero),
                Quantidade = quantidade,
                ValorTotal = valorTotal,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => operacaoService.RegistrarAsync(operacao), Erros()))
                await Formulario.VoltarAsync();
        }

        private async void Cancelar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();

        /// <summary>Label de erro de cada propriedade validada pelo caso de uso.</summary>
        private Dictionary<string, Label> Erros() => new()
        {
            [nameof(Operacao.DataOperacao)] = ErroData,
            [nameof(Operacao.Quantidade)] = ErroQuantidade,
            [nameof(Operacao.ValorTotal)] = ErroValorTotal,
        };
    }
}

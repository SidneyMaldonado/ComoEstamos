using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Investimentos
{
    /// <summary>Alteração de investimento. Carrega o investimento do <c>id</c> recebido; Salvar valida, grava e volta.</summary>
    public partial class AlterarInvestimentoPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "alterar-investimento";

        private readonly IInvestimentoService investimentoService;
        private readonly ICarteiraService carteiraService;

        private int idInvestimento;
        private bool carregada;
        private List<Carteira> carteiras = [];

        public AlterarInvestimentoPage(IInvestimentoService investimentoService, ICarteiraService carteiraService)
        {
            this.investimentoService = investimentoService;
            this.carteiraService = carteiraService;
            InitializeComponent();
        }

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

            carteiras = await carteiraService.BuscarTodasAsync();
            CampoCarteira.ItemsSource = carteiras.Select(c => c.Nome).ToList();

            CampoCarteira.SelectedIndex = carteiras.FindIndex(c => c.Id == investimento.IdCarteira);
            CampoNome.Text = investimento.Nome;
            CampoQuantidade.Text = FormatoBr.Numero(investimento.Quantidade, 6);
            CampoCotacao.Text = FormatoBr.Numero(investimento.Cotacao, 6);
            CampoObservacao.Text = investimento.Observacao;
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroCarteira, ErroNome, ErroQuantidade, ErroCotacao, ErroObservacao);
            // "&" (e não "&&") para mostrar o erro de formato de todos os campos de uma vez.
            var formatoValido = Formulario.TentarLerDecimal(CampoQuantidade, ErroQuantidade, "a quantidade", "1.000,5", out var quantidade)
                & Formulario.TentarLerDecimal(CampoCotacao, ErroCotacao, "a cotação", "32,50", out var cotacao);
            if (!formatoValido)
                return;

            var investimento = new Investimento
            {
                Id = idInvestimento,
                IdCarteira = CarteiraEscolhida()?.Id ?? 0,
                Nome = CampoNome.Text ?? string.Empty,
                Quantidade = quantidade,
                Cotacao = cotacao,
                Observacao = CampoObservacao.Text,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => investimentoService.AlterarAsync(investimento), Erros()))
                await Formulario.VoltarAsync();
        }

        private async void Cancelar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();

        /// <summary>Label de erro de cada propriedade validada pelo caso de uso.</summary>
        private Dictionary<string, Label> Erros() => new()
        {
            [nameof(Investimento.IdCarteira)] = ErroCarteira,
            [nameof(Investimento.Nome)] = ErroNome,
            [nameof(Investimento.Quantidade)] = ErroQuantidade,
            [nameof(Investimento.Cotacao)] = ErroCotacao,
            [nameof(Investimento.Observacao)] = ErroObservacao,
        };

        /// <summary>Carteira escolhida no Picker, ou <c>null</c> se nenhuma.</summary>
        private Carteira? CarteiraEscolhida() =>
            CampoCarteira.SelectedIndex >= 0 ? carteiras[CampoCarteira.SelectedIndex] : null;
    }
}

using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Investimentos
{
    /// <summary>Inclusão de investimento. Salvar valida, inclui e volta para a lista.</summary>
    public partial class IncluirInvestimentoPage : ContentPage
    {
        public const string Rota = "incluir-investimento";

        private readonly IInvestimentoService investimentoService;
        private readonly ICarteiraService carteiraService;

        private List<Carteira> carteiras = [];
        private bool carregada;

        public IncluirInvestimentoPage(IInvestimentoService investimentoService, ICarteiraService carteiraService)
        {
            this.investimentoService = investimentoService;
            this.carteiraService = carteiraService;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (carregada)
                return;
            carregada = true;

            carteiras = await carteiraService.BuscarTodasAsync();
            CampoCarteira.ItemsSource = carteiras.Select(c => c.Nome).ToList();
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
                IdCarteira = CarteiraEscolhida()?.Id ?? 0,
                Nome = CampoNome.Text ?? string.Empty,
                Quantidade = quantidade,
                Cotacao = cotacao,
                Observacao = CampoObservacao.Text,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => investimentoService.IncluirAsync(investimento), Erros()))
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

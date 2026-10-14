using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Parcelas
{
    /// <summary>
    /// Inclusão de uma parcela na dívida (ou receita) do <c>id</c> recebido (aberta pelo + da Visualizar Dívida ou Receita). Vem preenchida
    /// com o valor da última parcela, o vencimento do mês seguinte a ela, e a conta e a categoria da dívida.
    /// Salvar valida, grava e volta; o valor total da dívida passa a ser a soma das parcelas.
    /// </summary>
    public partial class IncluirParcelaPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "incluir-parcela";

        private readonly IParcelaService parcelaService;
        private readonly IDividaService dividaService;
        private readonly IContaService contaService;
        private readonly ICategoriaService categoriaService;

        private int idDivida;
        private bool carregada;
        private List<Conta> contas = [];
        private List<Categoria> categorias = [];

        public IncluirParcelaPage(IParcelaService parcelaService, IDividaService dividaService,
            IContaService contaService, ICategoriaService categoriaService)
        {
            this.parcelaService = parcelaService;
            this.dividaService = dividaService;
            this.contaService = contaService;
            this.categoriaService = categoriaService;
            InitializeComponent();
        }

        /// <summary>O <c>id</c> da rota é o da dívida.</summary>
        public void ApplyQueryAttributes(IDictionary<string, object> query) => idDivida = Formulario.LerId(query);

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (carregada)
                return;
            carregada = true;

            var divida = await dividaService.BuscarPorIdAsync(idDivida);
            if (divida is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Dívida não encontrada", "A dívida não existe mais ou foi excluída.");
                return;
            }

            contas = await contaService.BuscarTodasAsync();
            categorias = await categoriaService.BuscarTodasAsync();
            CampoConta.ItemsSource = contas.Select(c => c.Nome).ToList();
            CampoCategoria.ItemsSource = categorias.Select(c => c.Nome).ToList();

            Aviso.Text = $"Nova parcela de \"{divida.Nome}\". Ela entra na ordem do vencimento, e o valor total da " +
                $"{TextosParcela.Tipo(divida.EhDivida)} passa a ser a soma das parcelas.";
            var ultima = divida.Parcelas.LastOrDefault();
            if (ultima is not null)
                CampoValor.Text = FormatoBr.NumeroFixo(ultima.Valor, 2);
            CampoVencimento.Date = ultima is null ? DateTime.Today : MesSeguinte(ultima.DataVencimento, divida.DiaVencimento);
            CampoConta.SelectedIndex = contas.FindIndex(c => c.Id == divida.IdConta);
            CampoCategoria.SelectedIndex = categorias.FindIndex(c => c.Id == divida.IdCategoria);
        }

        /// <summary>O mês seguinte a <paramref name="data"/>, no dia de vencimento (ou no último dia do mês, se ele for menor).</summary>
        private static DateTime MesSeguinte(DateTime data, int dia)
        {
            var mes = data.AddMonths(1);
            return new DateTime(mes.Year, mes.Month, Math.Min(dia, DateTime.DaysInMonth(mes.Year, mes.Month)));
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroValor, ErroVencimento, ErroConta, ErroCategoria);
            if (!Formulario.TentarLerDecimal(CampoValor, ErroValor, "o valor da parcela", "300,00", out var valor))
                return;

            var parcela = new Parcela
            {
                IdDivida = idDivida,
                Valor = valor,
                DataVencimento = CampoVencimento.Date ?? DateTime.Today,
                // 0 = nada escolhido; o caso de uso pede a escolha.
                IdConta = CampoConta.SelectedIndex >= 0 ? contas[CampoConta.SelectedIndex].Id : 0,
                IdCategoria = CampoCategoria.SelectedIndex >= 0 ? categorias[CampoCategoria.SelectedIndex].Id : 0,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => parcelaService.IncluirAsync(parcela), Erros()))
                await Formulario.VoltarAsync();
        }

        private async void Cancelar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();

        /// <summary>Label de erro de cada propriedade validada pelo caso de uso.</summary>
        private Dictionary<string, Label> Erros() => new()
        {
            [nameof(Parcela.Valor)] = ErroValor,
            [nameof(Parcela.DataVencimento)] = ErroVencimento,
            [nameof(Parcela.IdConta)] = ErroConta,
            [nameof(Parcela.IdCategoria)] = ErroCategoria,
        };
    }
}

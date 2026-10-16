using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Parcelas
{
    /// <summary>
    /// Alteração de uma parcela em aberto: valor, vencimento e conta (a categoria é a da dívida). Salvar valida, grava e volta;
    /// o valor total da dívida (ou receita) passa a ser a soma das parcelas.
    /// </summary>
    public partial class AlterarParcelaPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "alterar-parcela";

        private readonly IParcelaService parcelaService;
        private readonly IContaService contaService;

        private int idParcela;
        private bool carregada;
        private List<Conta> contas = [];

        public AlterarParcelaPage(IParcelaService parcelaService, IContaService contaService)
        {
            this.parcelaService = parcelaService;
            this.contaService = contaService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idParcela = Formulario.LerId(query);

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (carregada)
                return;
            carregada = true;

            var parcela = await parcelaService.BuscarPorIdAsync(idParcela);
            if (parcela is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Parcela não encontrada", "A parcela não existe mais ou foi excluída.");
                return;
            }
            var ehDivida = parcela.Divida?.EhDivida != false;
            if (parcela.DataPagamento is not null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, ehDivida ? "Parcela paga" : "Parcela recebida",
                    ehDivida ? "A parcela já está paga e não pode ser alterada." : "A parcela já foi recebida e não pode ser alterada.");
                return;
            }

            contas = await contaService.BuscarTodasAsync();
            CampoConta.ItemsSource = contas.Select(c => c.Nome).ToList();

            Aviso.Text = $"{parcela.Descricao}. O valor total da {TextosParcela.Tipo(ehDivida)} passa a ser a soma das parcelas.";
            CampoValor.Text = FormatoBr.NumeroFixo(parcela.Valor, 2);
            CampoVencimento.Date = parcela.DataVencimento;
            CampoConta.SelectedIndex = contas.FindIndex(c => c.Id == parcela.IdConta);
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroValor, ErroVencimento, ErroConta);
            if (!Formulario.TentarLerDecimal(CampoValor, ErroValor, "o valor da parcela", "300,00", out var valor))
                return;

            var parcela = new Parcela
            {
                Id = idParcela,
                Valor = valor,
                DataVencimento = CampoVencimento.Date ?? DateTime.Today,
                // 0 = nada escolhido; o caso de uso pede a escolha.
                IdConta = CampoConta.SelectedIndex >= 0 ? contas[CampoConta.SelectedIndex].Id : 0,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => parcelaService.AlterarAsync(parcela), Erros()))
                await Formulario.VoltarAsync();
        }

        private async void Cancelar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();

        /// <summary>Label de erro de cada propriedade validada pelo caso de uso.</summary>
        private Dictionary<string, Label> Erros() => new()
        {
            [nameof(Parcela.Valor)] = ErroValor,
            [nameof(Parcela.DataVencimento)] = ErroVencimento,
            [nameof(Parcela.IdConta)] = ErroConta,
        };
    }
}

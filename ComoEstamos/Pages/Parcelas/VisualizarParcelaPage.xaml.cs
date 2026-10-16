using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Parcelas
{
    /// <summary>
    /// Visualização da parcela, aberta ao tocar numa parcela da Visualizar Dívida (ou Receita). Na parcela em aberto:
    /// Pagar (na receita, Receber) com a data, o lápis abre a alteração e a lixeira exclui. A parcela paga não muda.
    /// </summary>
    public partial class VisualizarParcelaPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "visualizar-parcela";

        private const double OpacidadeDesabilitado = 0.35;

        private readonly IParcelaService parcelaService;

        private int idParcela;
        private Parcela? parcela;

        public VisualizarParcelaPage(IParcelaService parcelaService)
        {
            this.parcelaService = parcelaService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idParcela = Formulario.LerId(query);

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarAsync();
        }

        private async Task CarregarAsync()
        {
            parcela = await parcelaService.BuscarPorIdAsync(idParcela);
            if (parcela is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Parcela não encontrada", "A parcela não existe mais ou foi excluída.");
                return;
            }

            var parcelas = parcela.Divida?.Parcelas ?? [];
            var numero = parcelas.FindIndex(p => p.Id == parcela.Id) + 1;
            var paga = parcela.DataPagamento is not null;
            var ehDivida = EhDivida;

            TituloDivida.Text = ehDivida ? "Dívida" : "Receita";
            TituloDataPagamento.Text = TituloCampoDataPagamento.Text = ehDivida ? "Data do Pagamento" : "Data do Recebimento";
            BotaoPagar.Text = ehDivida ? "Pagar" : "Receber";
            ValorDivida.Text = parcela.Divida?.Nome ?? "—";
            ValorNumero.Text = numero > 0 ? $"{numero} de {parcelas.Count}" : "—";
            ValorValor.Text = FormatoBr.Moeda(parcela.Valor);
            ValorVencimento.Text = FormatoBr.Data(parcela.DataVencimento);
            ValorSituacao.Text = TextosParcela.Situacao(parcela, ehDivida);
            ValorDataPagamento.Text = parcela.DataPagamento is DateTime pagamento ? FormatoBr.Data(pagamento) : "—";
            ValorConta.Text = parcela.Conta?.Nome ?? "—";
            ValorCategoria.Text = parcela.Divida?.Categoria?.Nome ?? "—";
            ValorDescricao.Text = parcela.Descricao;
            ValorDataCriacao.Text = FormatoBr.DataHora(parcela.DataCriacao);
            ValorDataAlteracao.Text = FormatoBr.DataHora(parcela.DataAlteracao);

            // Parcela paga: lápis e lixeira apagados e sem efeito; sem o cartão de pagamento.
            BotaoAlterar.Opacity = BotaoExcluir.Opacity = paga ? OpacidadeDesabilitado : 1;
            CartaoPagar.IsVisible = !paga;
            CampoDataPagamento.MaximumDate = DateTime.Today;
            CampoDataPagamento.Date = DateTime.Today;
            Formulario.LimparErros(ErroDataPagamento);
        }

        /// <summary>A parcela é de dívida; <c>false</c> quando é de receita (crédito).</summary>
        private bool EhDivida => parcela?.Divida?.EhDivida != false;

        private bool EmAberto => parcela is { DataPagamento: null };

        private async void Alterar_Tapped(object? sender, TappedEventArgs e)
        {
            if (EmAberto)
                await Shell.Current.GoToAsync($"{AlterarParcelaPage.Rota}?id={parcela!.Id}");
        }

        private async void Excluir_Tapped(object? sender, TappedEventArgs e)
        {
            if (!EmAberto)
                return;

            var id = parcela!.Id;
            var pergunta = $"Confirma a exclusão da parcela \"{parcela.Descricao}\" ({FormatoBr.Moeda(parcela.Valor)})? " +
                $"A {TextosParcela.Tipo(EhDivida)} fica com uma parcela a menos e o valor total diminui.";
            await Formulario.ConfirmarEExcluirAsync(this, "Excluir parcela", pergunta, () => parcelaService.ExcluirAsync(id));
        }

        private async void Pagar_Clicked(object? sender, EventArgs e)
        {
            if (!EmAberto)
                return;

            Formulario.LimparErros(ErroDataPagamento);
            var data = CampoDataPagamento.Date ?? DateTime.Today;
            var (titulo, acao) = EhDivida ? ("Pagar parcela", "o pagamento") : ("Receber parcela", "o recebimento");
            var pergunta = $"Confirma {acao} de {FormatoBr.Moeda(parcela!.Valor)} em {FormatoBr.Data(data)}?";
            if (!await DisplayAlertAsync(titulo, pergunta, "Sim", "Não"))
                return;

            var id = parcela.Id;
            var erros = new Dictionary<string, Label> { [nameof(Parcela.DataPagamento)] = ErroDataPagamento };
            if (await Formulario.SalvarAsync(this, BotaoPagar, () => parcelaService.PagarAsync(id, data), erros))
                await CarregarAsync();
        }

        private async void Voltar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();
    }
}

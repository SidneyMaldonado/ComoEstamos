using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Parcelas
{
    /// <summary>
    /// Visualização da parcela, aberta ao tocar numa parcela da Visualizar Dívida. Na parcela em aberto:
    /// Pagar (com a data), o lápis abre a alteração e a lixeira exclui. A parcela paga não muda.
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

            ValorDivida.Text = parcela.Divida?.Nome ?? "—";
            ValorNumero.Text = numero > 0 ? $"{numero} de {parcelas.Count}" : "—";
            ValorValor.Text = FormatoBr.Moeda(parcela.Valor);
            ValorVencimento.Text = FormatoBr.Data(parcela.DataVencimento);
            ValorSituacao.Text = Situacao(parcela);
            ValorDataPagamento.Text = parcela.DataPagamento is DateTime pagamento ? FormatoBr.Data(pagamento) : "—";
            ValorConta.Text = parcela.Conta?.Nome ?? "—";
            ValorCategoria.Text = parcela.Categoria?.Nome ?? "—";
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

        /// <summary>"Paga em 05/10/2026", "Vencida" ou "Em aberto" (como na tabela da Visualizar Dívida).</summary>
        private static string Situacao(Parcela parcela) =>
            parcela.DataPagamento is DateTime pagamento ? $"Paga em {FormatoBr.Data(pagamento)}"
            : parcela.DataVencimento.Date < DateTime.Today ? "Vencida"
            : "Em aberto";

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
                "A dívida fica com uma parcela a menos e o valor total diminui.";
            await Formulario.ConfirmarEExcluirAsync(this, "Excluir parcela", pergunta, () => parcelaService.ExcluirAsync(id));
        }

        private async void Pagar_Clicked(object? sender, EventArgs e)
        {
            if (!EmAberto)
                return;

            Formulario.LimparErros(ErroDataPagamento);
            var data = CampoDataPagamento.Date ?? DateTime.Today;
            var efeito = parcela!.Divida?.EhDivida == false ? "somado ao" : "descontado do";
            var pergunta = $"Confirma o pagamento de {FormatoBr.Moeda(parcela.Valor)} em {FormatoBr.Data(data)}? " +
                $"O valor será {efeito} saldo da conta \"{parcela.Conta?.Nome}\".";
            if (!await DisplayAlertAsync("Pagar parcela", pergunta, "Sim", "Não"))
                return;

            var id = parcela.Id;
            var erros = new Dictionary<string, Label> { [nameof(Parcela.DataPagamento)] = ErroDataPagamento };
            if (await Formulario.SalvarAsync(this, BotaoPagar, () => parcelaService.PagarAsync(id, data), erros))
                await CarregarAsync();
        }

        private async void Voltar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();
    }
}

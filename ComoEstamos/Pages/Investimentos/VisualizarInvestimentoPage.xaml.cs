using ComoEstamos.Controls;
using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Investimentos
{
    /// <summary>
    /// Visualização do investimento, só de leitura, com as últimas operações embaixo. O lápis abre a
    /// alteração; a lixeira pede confirmação e exclui (desativa). O + da seção abre a Incluir Operação;
    /// tocar numa operação pede confirmação e a exclui (não há alteração de operação).
    /// </summary>
    public partial class VisualizarInvestimentoPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "visualizar-investimento";

        /// <summary>Quantas operações, das mais recentes, a tabela mostra.</summary>
        private const int OperacoesExibidas = 10;

        private readonly IInvestimentoService investimentoService;
        private readonly IOperacaoService operacaoService;

        private int idInvestimento;
        private Investimento? investimento;
        private List<Operacao> operacoes = [];

        public VisualizarInvestimentoPage(IInvestimentoService investimentoService, IOperacaoService operacaoService)
        {
            this.investimentoService = investimentoService;
            this.operacaoService = operacaoService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idInvestimento = Formulario.LerId(query);

        /// <summary>Recarrega sempre, para refletir o que foi alterado na página Alterar ou na Incluir Operação.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarAsync();
        }

        private async Task CarregarAsync()
        {
            investimento = await investimentoService.BuscarPorIdAsync(idInvestimento);
            if (investimento is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Investimento não encontrado", "O investimento não existe mais ou foi excluído.");
                return;
            }

            ValorCarteira.Text = investimento.Carteira?.Nome;
            ValorNome.Text = investimento.Nome;
            ValorQuantidade.Text = FormatoBr.Numero(investimento.Quantidade, 6);
            ValorCotacao.Text = FormatoBr.Moeda(investimento.Cotacao, 6);
            ValorObservacao.Text = investimento.Observacao ?? "—";
            ValorValorTotal.Text = FormatoBr.Moeda(investimento.Quantidade * investimento.Cotacao);
            ValorDataCriacao.Text = FormatoBr.DataHora(investimento.DataCriacao);
            ValorDataAlteracao.Text = FormatoBr.DataHora(investimento.DataAlteracao);

            operacoes = await operacaoService.BuscarUltimasAsync(investimento.Id, OperacoesExibidas);
            TabelaOperacoes.Linhas = LinhaTabela.Montar(
                operacoes,
                o => o.Id,
                o => $"{Tipo(o)} de {FormatoBr.Numero(o.Quantidade, 6)}",
                o => FormatoBr.DataHora(o.DataOperacao),
                o => o.ValorTotal);
        }

        private static string Tipo(Operacao operacao) => operacao.EhCompra ? "Compra" : "Venda";

        private async void Alterar_Tapped(object? sender, TappedEventArgs e)
        {
            if (investimento is not null)
                await Shell.Current.GoToAsync($"{AlterarInvestimentoPage.Rota}?id={investimento.Id}");
        }

        private async void Excluir_Tapped(object? sender, TappedEventArgs e)
        {
            if (investimento is null)
                return;

            var id = investimento.Id;
            await Formulario.ConfirmarEExcluirAsync(this,
                "Excluir investimento", $"Confirma a exclusão do investimento \"{investimento.Nome}\"?",
                () => investimentoService.ExcluirAsync(id));
        }

        private async void IncluirOperacao_Tapped(object? sender, TappedEventArgs e)
        {
            if (investimento is not null)
                await Shell.Current.GoToAsync($"{IncluirOperacaoPage.Rota}?id={investimento.Id}");
        }

        /// <summary>Não há alteração de operação: tocar nela vai direto para a exclusão, que desfaz o efeito na quantidade.</summary>
        private async void TabelaOperacoes_LinhaTocada(object? sender, LinhaTabela linha)
        {
            var operacao = operacoes.Find(o => o.Id == linha.Id);
            if (investimento is null || operacao is null)
                return;

            var quantidade = investimento.Quantidade + (operacao.EhCompra ? -operacao.Quantidade : operacao.Quantidade);
            var pergunta = $"Confirma a exclusão da {Tipo(operacao).ToLowerInvariant()} de {FormatoBr.Numero(operacao.Quantidade, 6)} " +
                $"em {FormatoBr.DataHora(operacao.DataOperacao)} ({FormatoBr.Moeda(operacao.ValorTotal)})? A quantidade do " +
                $"investimento passa de {FormatoBr.Numero(investimento.Quantidade, 6)} para {FormatoBr.Numero(quantidade, 6)}.";
            if (await Formulario.ConfirmarEExcluirAsync(this, "Excluir operação", pergunta,
                    () => operacaoService.ExcluirAsync(operacao.Id), voltar: false))
                await CarregarAsync();
        }

        private async void Voltar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();
    }
}

using ComoEstamos.Controls;
using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;
using ComoEstamos.Pages.Parcelas;

namespace ComoEstamos.Pages.Receitas
{
    /// <summary>
    /// Visualização da receita, só de leitura: os dados em cima e a lista das parcelas embaixo.
    /// O lápis abre a alteração; a lixeira pede confirmação e exclui a receita e as parcelas. Uma dívida não abre aqui.
    /// Tocar numa parcela abre a Visualizar Parcela; o + da seção Parcelas abre a Incluir Parcela.
    /// </summary>
    public partial class VisualizarReceitaPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "visualizar-receita";

        private readonly IDividaService dividaService;

        private int idDivida;
        private Divida? divida;

        public VisualizarReceitaPage(IDividaService dividaService)
        {
            this.dividaService = dividaService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idDivida = Formulario.LerId(query);

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            divida = await dividaService.BuscarPorIdAsync(idDivida);
            // Uma dívida não abre aqui: ela tem a tela própria.
            if (divida is not { EhDivida: false })
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Receita não encontrada", "A receita não existe mais ou foi excluída.");
                return;
            }

            var parcelas = divida.Parcelas;
            var recebidas = parcelas.Count(p => p.DataPagamento is not null);

            ValorNome.Text = divida.Nome;
            ValorCredor.Text = divida.Credor?.Nome ?? "—";
            ValorConta.Text = divida.Conta?.Nome ?? "—";
            ValorCategoria.Text = divida.Categoria?.Nome ?? "—";
            ValorTotal.Text = FormatoBr.Moeda(divida.Valor);
            // Valor da próxima parcela em aberto: depois de uma alteração, pode ser diferente do das recebidas.
            var referencia = parcelas.FirstOrDefault(p => p.DataPagamento is null) ?? parcelas.FirstOrDefault();
            ValorParcelas.Text = $"{divida.NumeroParcelas}x de {FormatoBr.Moeda(referencia?.Valor ?? divida.Valor)}";
            ValorSituacao.Text = parcelas.Count > 0 && recebidas == parcelas.Count ? "Recebida" : $"{recebidas} de {parcelas.Count} recebidas";
            ValorDiaVencimento.Text = divida.DiaVencimento.ToString(FormatoBr.Cultura);
            ValorPrimeiroVencimento.Text = FormatoBr.Data(divida.DataPrimeiroVencimento);
            ValorDataCriacao.Text = FormatoBr.DataHora(divida.DataCriacao);
            ValorDataAlteracao.Text = FormatoBr.DataHora(divida.DataAlteracao);

            var numeradas = parcelas.Select((p, i) => (parcela: p, numero: i + 1)).ToList();
            TabelaParcelas.Linhas = LinhaTabela.Montar(
                numeradas,
                n => n.parcela.Id,
                n => $"{n.numero}/{parcelas.Count} · {FormatoBr.Data(n.parcela.DataVencimento)}",
                n => TextosParcela.Situacao(n.parcela, ehDivida: false),
                n => n.parcela.Valor);
        }

        private async void Alterar_Tapped(object? sender, TappedEventArgs e)
        {
            if (divida is not null)
                await Shell.Current.GoToAsync($"{AlterarReceitaPage.Rota}?id={divida.Id}");
        }

        private async void Excluir_Tapped(object? sender, TappedEventArgs e)
        {
            if (divida is null)
                return;

            var id = divida.Id;
            var pergunta = $"Confirma a exclusão da receita \"{divida.Nome}\" e de todas as parcelas?";
            await Formulario.ConfirmarEExcluirAsync(this, "Excluir receita", pergunta, () => dividaService.ExcluirAsync(id));
        }

        private async void IncluirParcela_Tapped(object? sender, TappedEventArgs e)
        {
            if (divida is not null)
                await Shell.Current.GoToAsync($"{IncluirParcelaPage.Rota}?id={divida.Id}");
        }

        private async void TabelaParcelas_LinhaTocada(object? sender, LinhaTabela linha) =>
            await Shell.Current.GoToAsync($"{VisualizarParcelaPage.Rota}?id={linha.Id}");

        private async void Voltar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();
    }
}

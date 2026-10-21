using ComoEstamos.Controls;
using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Pages.Dividas;
using ComoEstamos.Pages.Receitas;

namespace ComoEstamos.Pages.Resumo
{
    /// <summary>
    /// Resumo do mês: as dívidas e as receitas (créditos) com parcela vencendo no mês, com os totais e a
    /// diferença. As setas mudam o mês; tocar numa linha abre a dívida ou a receita. Aberta pelo menu e pelo arrastar
    /// para cima da tela inicial; arrastar para cima de novo abre as dívidas, e para baixo volta ao início.
    /// </summary>
    public partial class ResumoMesPage : ContentPage
    {
        private readonly IDividaService dividaService;

        /// <summary>Mês exibido (dia 1). Começa no mês atual e continua ao voltar para a página.</summary>
        private DateTime mes = MesAtual();

        /// <summary>Número da consulta mais recente: respostas de consultas anteriores são descartadas.</summary>
        private int consultaAtual;

        public ResumoMesPage(IDividaService dividaService)
        {
            this.dividaService = dividaService;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarAsync();
        }

        private static DateTime MesAtual() => new(DateTime.Today.Year, DateTime.Today.Month, 1);

        private async Task CarregarAsync()
        {
            var nomeMes = Maiuscula(FormatoBr.Cultura.DateTimeFormat.GetMonthName(mes.Month));
            TextoMes.Text = Maiuscula(FormatoBr.MesAno(mes));
            TabelaDespesas.TituloValor = TabelaReceitas.TituloValor = TabelaResumo.TituloValor = nomeMes;

            var consulta = ++consultaAtual;
            var resumo = await dividaService.BuscarResumoMesAsync(mes);
            if (consulta != consultaAtual)
                return;

            TabelaDespesas.Linhas = Linhas(resumo.Despesas, "Paga");
            TabelaDespesas.Total = resumo.TotalDespesas;
            TabelaReceitas.Linhas = Linhas(resumo.Receitas, "Recebida");
            TabelaReceitas.Total = resumo.TotalReceitas;
            // Linhas com Id 0: não abrem nada ao serem tocadas.
            TabelaResumo.Linhas = LinhaTabela.Montar(
                new[] { (Nome: "Receitas", Valor: resumo.TotalReceitas), (Nome: "Despesas", Valor: resumo.TotalDespesas) },
                _ => 0,
                l => l.Nome,
                valor: l => l.Valor);
            TabelaResumo.Total = resumo.Diferenca;
        }

        /// <param name="paga">"Paga" nas dívidas; "Recebida" nas receitas.</param>
        private static List<LinhaTabela> Linhas(IEnumerable<ItemResumoMes> itens, string paga) =>
            LinhaTabela.Montar(itens, i => i.IdDivida, i => i.Nome, i => Detalhe(i, paga), i => i.Valor, i => i.Paga);

        /// <summary>"Paga em 05/10" (ou "Recebida em"), "Vencida em 10/10" ou "Vence em 10/10".</summary>
        private static string Detalhe(ItemResumoMes item, string paga) =>
            item.DataPagamento is DateTime pagamento ? $"{paga} em {DiaMes(pagamento)}"
            : item.Vencimento.Date < DateTime.Today ? $"Vencida em {DiaMes(item.Vencimento)}"
            : $"Vence em {DiaMes(item.Vencimento)}";

        private static string DiaMes(DateTime data) => data.ToString("dd/MM", FormatoBr.Cultura);

        private static string Maiuscula(string texto) => char.ToUpper(texto[0], FormatoBr.Cultura) + texto[1..];

        private async Task MudarMesAsync(int meses)
        {
            mes = mes.AddMonths(meses);
            await CarregarAsync();
        }

        private async void MesAnterior_Clicked(object? sender, EventArgs e) => await MudarMesAsync(-1);

        private async void ProximoMes_Clicked(object? sender, EventArgs e) => await MudarMesAsync(1);

        private async void Mes_Tapped(object? sender, TappedEventArgs e)
        {
            if (mes == MesAtual())
                return;
            mes = MesAtual();
            await CarregarAsync();
        }

        private async void Tabela_LinhaTocada(object? sender, LinhaTabela linha)
        {
            if (linha.Id > 0)
                await Shell.Current.GoToAsync($"{VisualizarDividaPage.Rota}?id={linha.Id}");
        }

        private async void TabelaReceitas_LinhaTocada(object? sender, LinhaTabela linha)
        {
            if (linha.Id > 0)
                await Shell.Current.GoToAsync($"{VisualizarReceitaPage.Rota}?id={linha.Id}");
        }

        private void Menu_Clicked(object? sender, EventArgs e) => Shell.Current.FlyoutIsPresented = true;

        private async void ArrastarParaCima_Swiped(object? sender, SwipedEventArgs e) => await AbrirDividasAsync();

        private async void Rolagem_ArrastouParaCimaNoFim(object? sender, EventArgs e) => await AbrirDividasAsync();

        /// <summary>Abre a lista de dívidas como o item "Dívidas" do menu (rota absoluta do Shell).</summary>
        private static Task AbrirDividasAsync() => Shell.Current.GoToAsync("//dividas");

        private async void ArrastarParaBaixo_Swiped(object? sender, SwipedEventArgs e) => await VoltarAoInicioAsync();

        private async void Rolagem_ArrastouParaBaixoNoInicio(object? sender, EventArgs e) => await VoltarAoInicioAsync();

        /// <summary>Volta para a tela inicial, como o item "Início" do menu.</summary>
        private static Task VoltarAoInicioAsync() => Shell.Current.GoToAsync("//MainPage");
    }
}

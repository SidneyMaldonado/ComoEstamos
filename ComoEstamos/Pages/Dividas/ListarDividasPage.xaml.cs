using ComoEstamos.Controls;
using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases.Dividas;

namespace ComoEstamos.Pages.Dividas
{
    /// <summary>
    /// Consulta das dívidas, ordenada pelo próximo vencimento, com filtro por nome, por mês de
    /// vencimento e para ocultar as pagas. Tocar numa linha abre a visualização; o "+" abre a inclusão.
    /// </summary>
    public partial class ListarDividasPage : ContentPage
    {
        private readonly IDividaService dividaService;

        /// <summary>Mês filtrado (dia 1); <c>null</c> mostra todos os meses.</summary>
        private DateTime? mesFiltro;

        /// <summary>Número da consulta mais recente: respostas de consultas anteriores são descartadas.</summary>
        private int consultaAtual;

        public ListarDividasPage(IDividaService dividaService)
        {
            this.dividaService = dividaService;
            InitializeComponent();
            MostrarMes();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarAsync();
        }

        private async Task CarregarAsync()
        {
            var consulta = ++consultaAtual;
            var filtro = new FiltroDividas(CampoNome.Text, mesFiltro, CaixaOcultarPagas.IsChecked);
            var dividas = await dividaService.BuscarAsync(filtro);
            if (consulta != consultaAtual)
                return;

            var temFiltro = !string.IsNullOrWhiteSpace(filtro.Nome) || filtro.MesVencimento is not null || filtro.OcultarPagas;
            Tabela.MensagemVazia = temFiltro ? "Nenhuma dívida encontrada." : "Nenhuma dívida cadastrada.";
            Tabela.Linhas = LinhaTabela.Montar(dividas, d => d.Id, d => d.Nome, Detalhe, d => d.ValorTotal);
        }

        /// <summary>"Dia 10 · 12x de R$ 100,00", mais " · Paga" quando todas as parcelas estão pagas.</summary>
        private static string Detalhe(ResumoDivida d) =>
            $"Dia {d.DiaVencimento} · {d.NumeroParcelas}x de {FormatoBr.Moeda(d.ValorParcela)}" + (d.Paga ? " · Paga" : "");

        private void MostrarMes()
        {
            TextoMes.Text = mesFiltro is DateTime mes ? Maiuscula(FormatoBr.MesAno(mes)) : "Todos os meses";
            BotaoTodosMeses.IsVisible = mesFiltro is not null;
        }

        private static string Maiuscula(string texto) => char.ToUpper(texto[0], FormatoBr.Cultura) + texto[1..];

        /// <summary>Muda o mês filtrado. Sem mês escolhido, as setas e o toque começam pelo mês atual.</summary>
        private async Task MudarMesAsync(int meses)
        {
            var hoje = DateTime.Today;
            mesFiltro = mesFiltro?.AddMonths(meses) ?? new DateTime(hoje.Year, hoje.Month, 1);
            MostrarMes();
            await CarregarAsync();
        }

        private async void MesAnterior_Clicked(object? sender, EventArgs e) => await MudarMesAsync(-1);

        private async void ProximoMes_Clicked(object? sender, EventArgs e) => await MudarMesAsync(1);

        private async void Mes_Tapped(object? sender, TappedEventArgs e)
        {
            if (mesFiltro is null)
                await MudarMesAsync(0);
        }

        private async void TodosMeses_Clicked(object? sender, EventArgs e)
        {
            mesFiltro = null;
            MostrarMes();
            await CarregarAsync();
        }

        private async void Filtro_Alterado(object? sender, TextChangedEventArgs e) => await CarregarAsync();

        private async void OcultarPagas_Alterado(object? sender, CheckedChangedEventArgs e) => await CarregarAsync();

        private void OcultarPagas_Tapped(object? sender, TappedEventArgs e) =>
            CaixaOcultarPagas.IsChecked = !CaixaOcultarPagas.IsChecked;

        private void Menu_Clicked(object? sender, EventArgs e) => Shell.Current.FlyoutIsPresented = true;

        /// <summary>Volta para o resumo do mês, como o item "Resumo do Mês" do menu.</summary>
        private async void ArrastarParaBaixo_Swiped(object? sender, SwipedEventArgs e) =>
            await Shell.Current.GoToAsync("//resumo");

        private async void Incluir_Tapped(object? sender, TappedEventArgs e) =>
            await Shell.Current.GoToAsync(IncluirDividaPage.Rota);

        private async void Tabela_LinhaTocada(object? sender, LinhaTabela linha) =>
            await Shell.Current.GoToAsync($"{VisualizarDividaPage.Rota}?id={linha.Id}");
    }
}

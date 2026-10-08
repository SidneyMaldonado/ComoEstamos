using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Investimentos
{
    /// <summary>
    /// Visualização do investimento, só de leitura. O lápis abre a alteração; a lixeira pede
    /// confirmação e exclui (desativa).
    /// </summary>
    public partial class VisualizarInvestimentoPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "visualizar-investimento";

        private readonly IInvestimentoService investimentoService;

        private int idInvestimento;
        private Investimento? investimento;

        public VisualizarInvestimentoPage(IInvestimentoService investimentoService)
        {
            this.investimentoService = investimentoService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idInvestimento = Formulario.LerId(query);

        /// <summary>Recarrega sempre, para refletir o que foi alterado na página Alterar.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
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
        }

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

        private async void Voltar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();
    }
}

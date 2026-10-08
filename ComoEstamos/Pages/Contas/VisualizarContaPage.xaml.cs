using ComoEstamos.Controls;
using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Contas
{
    /// <summary>
    /// Visualização da conta, só de leitura. O lápis abre a alteração; a lixeira pede
    /// confirmação e exclui (desativa).
    /// </summary>
    public partial class VisualizarContaPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "visualizar-conta";

        private readonly IContaService contaService;

        private int idConta;
        private Conta? conta;

        public VisualizarContaPage(IContaService contaService)
        {
            this.contaService = contaService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idConta = Formulario.LerId(query);

        /// <summary>Recarrega sempre, para refletir o que foi alterado na página Alterar.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            conta = await contaService.BuscarPorIdAsync(idConta);
            if (conta is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Conta não encontrada", "A conta não existe mais ou foi excluída.");
                return;
            }

            ValorNome.Text = conta.Nome;
            ValorSaldo.Text = FormatoBr.Moeda(conta.Saldo);
            ValorSaldo.TextColor = LinhaTabela.CorDoValor(conta.Saldo);
            ValorDataCriacao.Text = FormatoBr.DataHora(conta.DataCriacao);
            ValorDataAlteracao.Text = FormatoBr.DataHora(conta.DataAlteracao);
        }

        private async void Alterar_Tapped(object? sender, TappedEventArgs e)
        {
            if (conta is not null)
                await Shell.Current.GoToAsync($"{AlterarContaPage.Rota}?id={conta.Id}");
        }

        private async void Excluir_Tapped(object? sender, TappedEventArgs e)
        {
            if (conta is null)
                return;

            var id = conta.Id;
            await Formulario.ConfirmarEExcluirAsync(this,
                "Excluir conta", $"Confirma a exclusão da conta \"{conta.Nome}\"?",
                () => contaService.ExcluirAsync(id));
        }

        private async void Voltar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();
    }
}

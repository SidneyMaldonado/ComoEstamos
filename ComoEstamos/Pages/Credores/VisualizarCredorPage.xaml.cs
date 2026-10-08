using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Credores
{
    /// <summary>
    /// Visualização do credor, só de leitura. O lápis abre a alteração; a lixeira pede
    /// confirmação e exclui (desativa).
    /// </summary>
    public partial class VisualizarCredorPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "visualizar-credor";

        private readonly ICredorService credorService;

        private int idCredor;
        private Credor? credor;

        public VisualizarCredorPage(ICredorService credorService)
        {
            this.credorService = credorService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idCredor = Formulario.LerId(query);

        /// <summary>Recarrega sempre, para refletir o que foi alterado na página Alterar.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            credor = await credorService.BuscarPorIdAsync(idCredor);
            if (credor is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Credor não encontrado", "O credor não existe mais ou foi excluído.");
                return;
            }

            ValorNome.Text = credor.Nome;
            ValorObservacoes.Text = credor.Observacoes ?? "—";
            ValorDataCriacao.Text = FormatoBr.DataHora(credor.DataCriacao);
            ValorDataAlteracao.Text = FormatoBr.DataHora(credor.DataAlteracao);
        }

        private async void Alterar_Tapped(object? sender, TappedEventArgs e)
        {
            if (credor is not null)
                await Shell.Current.GoToAsync($"{AlterarCredorPage.Rota}?id={credor.Id}");
        }

        private async void Excluir_Tapped(object? sender, TappedEventArgs e)
        {
            if (credor is null)
                return;

            var id = credor.Id;
            await Formulario.ConfirmarEExcluirAsync(this,
                "Excluir credor", $"Confirma a exclusão do credor \"{credor.Nome}\"?",
                () => credorService.ExcluirAsync(id));
        }

        private async void Voltar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();
    }
}

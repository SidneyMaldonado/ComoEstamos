using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Categorias
{
    /// <summary>
    /// Visualização da categoria, só de leitura. O lápis abre a alteração; a lixeira pede
    /// confirmação e exclui (desativa).
    /// </summary>
    public partial class VisualizarCategoriaPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "visualizar-categoria";

        private readonly ICategoriaService categoriaService;

        private int idCategoria;
        private Categoria? categoria;

        public VisualizarCategoriaPage(ICategoriaService categoriaService)
        {
            this.categoriaService = categoriaService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idCategoria = Formulario.LerId(query);

        /// <summary>Recarrega sempre, para refletir o que foi alterado na página Alterar.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            categoria = await categoriaService.BuscarPorIdAsync(idCategoria);
            if (categoria is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Categoria não encontrada", "A categoria não existe mais ou foi excluída.");
                return;
            }

            ValorNome.Text = categoria.Nome;
            ValorDataCriacao.Text = FormatoBr.DataHora(categoria.DataCriacao);
            ValorDataAlteracao.Text = FormatoBr.DataHora(categoria.DataAlteracao);
        }

        private async void Alterar_Tapped(object? sender, TappedEventArgs e)
        {
            if (categoria is not null)
                await Shell.Current.GoToAsync($"{AlterarCategoriaPage.Rota}?id={categoria.Id}");
        }

        private async void Excluir_Tapped(object? sender, TappedEventArgs e)
        {
            if (categoria is null)
                return;

            var id = categoria.Id;
            await Formulario.ConfirmarEExcluirAsync(this,
                "Excluir categoria", $"Confirma a exclusão da categoria \"{categoria.Nome}\"?",
                () => categoriaService.ExcluirAsync(id));
        }

        private async void Voltar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();
    }
}

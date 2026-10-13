using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Categorias
{
    /// <summary>Inclusão de categoria. Salvar valida, inclui e volta para a lista.</summary>
    public partial class IncluirCategoriaPage : ContentPage
    {
        public const string Rota = "incluir-categoria";

        private readonly ICategoriaService categoriaService;

        public IncluirCategoriaPage(ICategoriaService categoriaService)
        {
            this.categoriaService = categoriaService;
            InitializeComponent();
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroNome);
            var categoria = new Categoria
            {
                Nome = CampoNome.Text ?? string.Empty,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => categoriaService.IncluirAsync(categoria), Erros()))
                await Formulario.VoltarAsync();
        }

        private async void Cancelar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();

        /// <summary>Label de erro de cada propriedade validada pelo caso de uso.</summary>
        private Dictionary<string, Label> Erros() => new()
        {
            [nameof(Categoria.Nome)] = ErroNome,
        };
    }
}

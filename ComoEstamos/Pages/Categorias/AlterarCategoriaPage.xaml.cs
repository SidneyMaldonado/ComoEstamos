using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Categorias
{
    /// <summary>Alteração de categoria. Carrega a categoria do <c>id</c> recebido; Salvar valida, grava e volta.</summary>
    public partial class AlterarCategoriaPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "alterar-categoria";

        private readonly ICategoriaService categoriaService;

        private int idCategoria;
        private bool carregada;

        public AlterarCategoriaPage(ICategoriaService categoriaService)
        {
            this.categoriaService = categoriaService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idCategoria = Formulario.LerId(query);

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (carregada)
                return;
            carregada = true;

            var categoria = await categoriaService.BuscarPorIdAsync(idCategoria);
            if (categoria is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Categoria não encontrada", "A categoria não existe mais ou foi excluída.");
                return;
            }

            CampoNome.Text = categoria.Nome;
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroNome);
            var categoria = new Categoria
            {
                Id = idCategoria,
                Nome = CampoNome.Text ?? string.Empty,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => categoriaService.AlterarAsync(categoria), Erros()))
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

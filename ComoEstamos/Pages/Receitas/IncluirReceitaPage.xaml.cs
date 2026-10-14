using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Receitas
{
    /// <summary>Inclusão de receita. Salvar valida, grava a receita com as parcelas e volta para a lista.</summary>
    public partial class IncluirReceitaPage : ContentPage
    {
        public const string Rota = "incluir-receita";

        private const string SemCredor = "Nenhum";

        private readonly IDividaService dividaService;
        private readonly IContaService contaService;
        private readonly ICategoriaService categoriaService;
        private readonly ICredorService credorService;

        private List<Conta> contas = [];
        private List<Categoria> categorias = [];
        private List<Credor> credores = [];
        private bool carregada;

        public IncluirReceitaPage(IDividaService dividaService, IContaService contaService,
            ICategoriaService categoriaService, ICredorService credorService)
        {
            this.dividaService = dividaService;
            this.contaService = contaService;
            this.categoriaService = categoriaService;
            this.credorService = credorService;
            InitializeComponent();
            CampoPrimeiroVencimento.Date = DateTime.Today;
            CampoDiaVencimento.Text = DateTime.Today.Day.ToString(FormatoBr.Cultura);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (carregada)
                return;
            carregada = true;

            contas = await contaService.BuscarTodasAsync();
            categorias = await categoriaService.BuscarTodasAsync();
            credores = await credorService.BuscarTodosAsync();
            CampoConta.ItemsSource = contas.Select(c => c.Nome).ToList();
            CampoCategoria.ItemsSource = categorias.Select(c => c.Nome).ToList();
            // A primeira opção é "Nenhum": o pagador é opcional. A lista é a dos credores.
            CampoCredor.ItemsSource = credores.Select(c => c.Nome).Prepend(SemCredor).ToList();
            CampoCredor.SelectedIndex = 0;
        }

        /// <summary>Ao escolher o primeiro vencimento, o dia das parcelas seguintes passa a ser o mesmo.</summary>
        private void PrimeiroVencimento_Selecionado(object? sender, DateChangedEventArgs e)
        {
            if (e.NewDate is DateTime data)
                CampoDiaVencimento.Text = data.Day.ToString(FormatoBr.Cultura);
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroNome, ErroValor, ErroNumeroParcelas, ErroDiaVencimento, ErroConta, ErroCategoria, ErroCredor);
            // "&" (e não "&&") para mostrar o erro de formato de todos os campos de uma vez.
            var formatoValido = Formulario.TentarLerDecimal(CampoValor, ErroValor, "o valor total", "3.600,00", out var valor)
                & Formulario.TentarLerInteiro(CampoNumeroParcelas, ErroNumeroParcelas, "o número de parcelas", out var numeroParcelas)
                & Formulario.TentarLerInteiro(CampoDiaVencimento, ErroDiaVencimento, "o dia de vencimento", out var diaVencimento);
            if (!formatoValido)
                return;

            var divida = new Divida
            {
                Nome = CampoNome.Text ?? string.Empty,
                Valor = valor,
                NumeroParcelas = numeroParcelas,
                DataPrimeiroVencimento = CampoPrimeiroVencimento.Date ?? DateTime.Today,
                DiaVencimento = diaVencimento,
                IdConta = Escolhido(CampoConta, contas)?.Id,
                IdCategoria = Escolhido(CampoCategoria, categorias)?.Id,
                // O índice 0 do Picker de pagador (credor) é "Nenhum".
                IdCredor = CampoCredor.SelectedIndex > 0 ? credores[CampoCredor.SelectedIndex - 1].Id : null,
                EhDivida = false,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => dividaService.IncluirAsync(divida), Erros()))
                await Formulario.VoltarAsync();
        }

        private async void Cancelar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();

        /// <summary>Label de erro de cada propriedade validada pelo caso de uso.</summary>
        private Dictionary<string, Label> Erros() => new()
        {
            [nameof(Divida.Nome)] = ErroNome,
            [nameof(Divida.Valor)] = ErroValor,
            [nameof(Divida.NumeroParcelas)] = ErroNumeroParcelas,
            [nameof(Divida.DiaVencimento)] = ErroDiaVencimento,
            [nameof(Divida.IdConta)] = ErroConta,
            [nameof(Divida.IdCategoria)] = ErroCategoria,
            [nameof(Divida.IdCredor)] = ErroCredor,
        };

        /// <summary>Item escolhido no Picker, ou <c>null</c> se nenhum.</summary>
        private static T? Escolhido<T>(Picker campo, List<T> itens) where T : class =>
            campo.SelectedIndex >= 0 ? itens[campo.SelectedIndex] : null;
    }
}

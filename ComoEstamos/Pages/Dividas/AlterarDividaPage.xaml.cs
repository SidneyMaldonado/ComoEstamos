using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Dividas
{
    /// <summary>
    /// Alteração de dívida. Carrega a dívida do <c>id</c> recebido; Salvar valida, grava e volta.
    /// Se houver parcelas pagas, o primeiro vencimento fica bloqueado e um aviso explica o que muda.
    /// </summary>
    public partial class AlterarDividaPage : ContentPage, IQueryAttributable
    {
        public const string Rota = "alterar-divida";

        private const string SemCredor = "Nenhum";

        private readonly IDividaService dividaService;
        private readonly IContaService contaService;
        private readonly ICategoriaService categoriaService;
        private readonly ICredorService credorService;

        private int idDivida;
        private bool carregada;
        private List<Conta> contas = [];
        private List<Categoria> categorias = [];
        private List<Credor> credores = [];

        public AlterarDividaPage(IDividaService dividaService, IContaService contaService,
            ICategoriaService categoriaService, ICredorService credorService)
        {
            this.dividaService = dividaService;
            this.contaService = contaService;
            this.categoriaService = categoriaService;
            this.credorService = credorService;
            InitializeComponent();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query) => idDivida = Formulario.LerId(query);

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (carregada)
                return;
            carregada = true;

            var divida = await dividaService.BuscarPorIdAsync(idDivida);
            if (divida is null)
            {
                await Formulario.AvisarNaoEncontradoAsync(this, "Dívida não encontrada", "A dívida não existe mais ou foi excluída.");
                return;
            }

            contas = await contaService.BuscarTodasAsync();
            categorias = await categoriaService.BuscarTodasAsync();
            credores = await credorService.BuscarTodosAsync();
            CampoConta.ItemsSource = contas.Select(c => c.Nome).ToList();
            CampoCategoria.ItemsSource = categorias.Select(c => c.Nome).ToList();
            // A primeira opção é "Nenhum": o credor é opcional.
            CampoCredor.ItemsSource = credores.Select(c => c.Nome).Prepend(SemCredor).ToList();

            CampoNome.Text = divida.Nome;
            CampoValor.Text = FormatoBr.NumeroFixo(divida.Valor, 2);
            CampoNumeroParcelas.Text = divida.NumeroParcelas.ToString(FormatoBr.Cultura);
            // A data antes do dia: escolher a data preenche o dia com o dela.
            CampoPrimeiroVencimento.Date = divida.DataPrimeiroVencimento;
            CampoDiaVencimento.Text = divida.DiaVencimento.ToString(FormatoBr.Cultura);
            CampoConta.SelectedIndex = contas.FindIndex(c => c.Id == divida.IdConta);
            CampoCategoria.SelectedIndex = categorias.FindIndex(c => c.Id == divida.IdCategoria);
            CampoCredor.SelectedIndex = credores.FindIndex(c => c.Id == divida.IdCredor) + 1;

            var pagas = divida.Parcelas.Where(p => p.DataPagamento is not null).ToList();
            if (pagas.Count > 0)
            {
                CampoPrimeiroVencimento.IsEnabled = false;
                AvisoPagas.Text = $"{pagas.Count} de {divida.Parcelas.Count} parcelas já pagas ({FormatoBr.Moeda(pagas.Sum(p => p.Valor))}). " +
                    "Elas não mudam. Se mudar o valor, o número de parcelas ou o dia, só as parcelas em aberto são refeitas, " +
                    "com o que falta pagar. Conta e categoria novas valem só para as parcelas em aberto.";
                AvisoPagas.IsVisible = true;
            }
        }

        /// <summary>Ao escolher o primeiro vencimento, o dia das parcelas seguintes passa a ser o mesmo.</summary>
        private void PrimeiroVencimento_Selecionado(object? sender, DateChangedEventArgs e)
        {
            if (e.NewDate is DateTime data)
                CampoDiaVencimento.Text = data.Day.ToString(FormatoBr.Cultura);
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros(ErroNome, ErroValor, ErroNumeroParcelas, ErroPrimeiroVencimento, ErroDiaVencimento,
                ErroConta, ErroCategoria, ErroCredor);
            // "&" (e não "&&") para mostrar o erro de formato de todos os campos de uma vez.
            var formatoValido = Formulario.TentarLerDecimal(CampoValor, ErroValor, "o valor total", "3.600,00", out var valor)
                & Formulario.TentarLerInteiro(CampoNumeroParcelas, ErroNumeroParcelas, "o número de parcelas", out var numeroParcelas)
                & Formulario.TentarLerInteiro(CampoDiaVencimento, ErroDiaVencimento, "o dia de vencimento", out var diaVencimento);
            if (!formatoValido)
                return;

            var divida = new Divida
            {
                Id = idDivida,
                Nome = CampoNome.Text ?? string.Empty,
                Valor = valor,
                NumeroParcelas = numeroParcelas,
                DataPrimeiroVencimento = CampoPrimeiroVencimento.Date ?? DateTime.Today,
                DiaVencimento = diaVencimento,
                IdConta = Escolhido(CampoConta, contas)?.Id,
                IdCategoria = Escolhido(CampoCategoria, categorias)?.Id,
                // O índice 0 do Picker de credor é "Nenhum".
                IdCredor = CampoCredor.SelectedIndex > 0 ? credores[CampoCredor.SelectedIndex - 1].Id : null,
            };
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => dividaService.AlterarAsync(divida), Erros()))
                await Formulario.VoltarAsync();
        }

        private async void Cancelar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();

        /// <summary>Label de erro de cada propriedade validada pelo caso de uso.</summary>
        private Dictionary<string, Label> Erros() => new()
        {
            [nameof(Divida.Nome)] = ErroNome,
            [nameof(Divida.Valor)] = ErroValor,
            [nameof(Divida.NumeroParcelas)] = ErroNumeroParcelas,
            [nameof(Divida.DataPrimeiroVencimento)] = ErroPrimeiroVencimento,
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

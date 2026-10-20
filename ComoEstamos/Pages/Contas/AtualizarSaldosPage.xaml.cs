using ComoEstamos.Controls;
using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases.Contas;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Contas
{
    /// <summary>
    /// Atualiza os saldos de todas as contas ativas de uma vez. Salvar grava só os que mudaram
    /// (com histórico); se algum saldo tiver erro, nada é gravado.
    /// </summary>
    public partial class AtualizarSaldosPage : ContentPage
    {
        public const string Rota = "atualizar-saldos";

        private readonly IContaService contaService;
        private readonly List<LinhaSaldo> linhas = [];

        private bool carregada;

        public AtualizarSaldosPage(IContaService contaService)
        {
            this.contaService = contaService;
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (carregada)
                return;
            carregada = true;

            var contas = await contaService.BuscarTodasAsync();
            foreach (var conta in contas)
                linhas.Add(MontarLinha(conta));
            MensagemVazia.IsVisible = contas.Count == 0;
            BotaoSalvar.IsEnabled = contas.Count > 0;
        }

        private LinhaSaldo MontarLinha(Conta conta)
        {
            var nome = new Label
            {
                Text = conta.Nome,
                Style = (Style)Application.Current!.Resources["TituloCampo"],
                VerticalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.TailTruncation,
            };
            var campo = new Entry
            {
                Text = FormatoBr.NumeroFixo(conta.Saldo, 2),
                Style = (Style)Application.Current.Resources["Campo"],
                Placeholder = "Ex.: 1.250,00",
                Keyboard = Keyboard.Numeric,
                HorizontalTextAlignment = TextAlignment.End,
                WidthRequest = 160,
            };
            campo.Behaviors.Add(new LimiteCasasDecimais());
            var erro = new Label { Style = (Style)Application.Current.Resources["MensagemErro"] };

            var grade = new Grid { ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)], ColumnSpacing = 12 };
            grade.Add(nome, 0);
            grade.Add(campo, 1);
            Linhas.Add(new VerticalStackLayout { Spacing = 4, Children = { grade, erro } });

            return new LinhaSaldo(conta.Id, campo, erro);
        }

        private async void Salvar_Clicked(object? sender, EventArgs e)
        {
            Formulario.LimparErros([.. linhas.Select(l => l.Erro)]);

            var saldos = new List<NovoSaldo>();
            var formatoOk = true;
            foreach (var linha in linhas)
            {
                if (Formulario.TentarLerDecimal(linha.Campo, linha.Erro, "o saldo", "1.250,00", out var saldo))
                    saldos.Add(new NovoSaldo(linha.IdConta, saldo));
                else
                    formatoOk = false;
            }
            if (!formatoOk)
                return;

            var erros = linhas.ToDictionary(l => AtualizarSaldosUseCase.CampoSaldo(l.IdConta), l => l.Erro);
            if (await Formulario.SalvarAsync(this, BotaoSalvar, () => contaService.AtualizarSaldosAsync(saldos), erros))
                await Formulario.VoltarAsync();
        }

        private async void Cancelar_Clicked(object? sender, EventArgs e) => await Formulario.VoltarAsync();

        private sealed record LinhaSaldo(int IdConta, Entry Campo, Label Erro);
    }
}

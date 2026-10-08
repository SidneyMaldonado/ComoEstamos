using ComoEstamos.Core.Formatacao;

namespace ComoEstamos.Controls
{
    /// <summary>Linha da tabela, já formatada para exibição.</summary>
    public record LinhaTabela(int Id, string Texto, string? Detalhe, string? Valor, Color CorValor, Color Fundo)
    {
        private static readonly Color CorValorPositivo = Color.FromArgb("#F2F4FA");
        private static readonly Color CorValorNegativo = Color.FromArgb("#FF9E9E");
        private static readonly Color FundoLinhaPar = Colors.Transparent;
        private static readonly Color FundoLinhaImpar = Color.FromArgb("#14FFFFFF");

        public bool TemDetalhe => !string.IsNullOrEmpty(Detalhe);

        public bool TemValor => Valor is not null;

        /// <summary>Cor de um valor em dinheiro: vermelho claro quando negativo.</summary>
        public static Color CorDoValor(decimal valor) => valor < 0 ? CorValorNegativo : CorValorPositivo;

        /// <summary>
        /// Monta as linhas na ordem recebida, alternando o fundo. <paramref name="detalhe"/> aparece
        /// em letra menor abaixo do texto; <paramref name="valor"/> aparece à direita, em reais.
        /// </summary>
        public static List<LinhaTabela> Montar<T>(
            IEnumerable<T> itens,
            Func<T, int> id,
            Func<T, string> texto,
            Func<T, string?>? detalhe = null,
            Func<T, decimal>? valor = null) =>
            itens.Select((item, i) => new LinhaTabela(
                id(item),
                texto(item),
                detalhe?.Invoke(item),
                valor is null ? null : FormatoBr.Moeda(valor(item)),
                valor is null ? CorValorPositivo : CorDoValor(valor(item)),
                i % 2 == 0 ? FundoLinhaPar : FundoLinhaImpar)).ToList();
    }

    /// <summary>
    /// Tabela das listas (MainPage e páginas Listar): uma coluna de texto, com detalhe opcional,
    /// e uma coluna de valor opcional, que só aparece quando <see cref="TituloValor"/> é informado.
    /// </summary>
    public partial class TabelaRegistros : ContentView
    {
        public static readonly BindableProperty TituloTextoProperty = BindableProperty.Create(
            nameof(TituloTexto), typeof(string), typeof(TabelaRegistros), string.Empty);

        public static readonly BindableProperty TituloValorProperty = BindableProperty.Create(
            nameof(TituloValor), typeof(string), typeof(TabelaRegistros), string.Empty,
            propertyChanged: (b, _, _) => ((TabelaRegistros)b).OnPropertyChanged(nameof(TemColunaValor)));

        public static readonly BindableProperty MensagemVaziaProperty = BindableProperty.Create(
            nameof(MensagemVazia), typeof(string), typeof(TabelaRegistros), string.Empty);

        public static readonly BindableProperty LinhasProperty = BindableProperty.Create(
            nameof(Linhas), typeof(IReadOnlyList<LinhaTabela>), typeof(TabelaRegistros),
            propertyChanged: (b, _, _) => ((TabelaRegistros)b).OnPropertyChanged(nameof(Vazia)));

        /// <summary>Título da coluna de texto (ex.: "Conta").</summary>
        public string TituloTexto
        {
            get => (string)GetValue(TituloTextoProperty);
            set => SetValue(TituloTextoProperty, value);
        }

        /// <summary>Título da coluna de valor (ex.: "Saldo"). Vazio esconde a coluna.</summary>
        public string TituloValor
        {
            get => (string)GetValue(TituloValorProperty);
            set => SetValue(TituloValorProperty, value);
        }

        /// <summary>Mensagem quando não há linhas (ex.: "Nenhuma conta cadastrada.").</summary>
        public string MensagemVazia
        {
            get => (string)GetValue(MensagemVaziaProperty);
            set => SetValue(MensagemVaziaProperty, value);
        }

        /// <summary>Linhas exibidas. Enquanto for nulo (ainda carregando), a mensagem de lista vazia não aparece.</summary>
        public IReadOnlyList<LinhaTabela>? Linhas
        {
            get => (IReadOnlyList<LinhaTabela>?)GetValue(LinhasProperty);
            set => SetValue(LinhasProperty, value);
        }

        public bool TemColunaValor => !string.IsNullOrEmpty(TituloValor);

        public bool Vazia => Linhas is { Count: 0 };

        /// <summary>Disparado quando uma linha é tocada.</summary>
        public event EventHandler<LinhaTabela>? LinhaTocada;

        public TabelaRegistros()
        {
            InitializeComponent();
        }

        private void Linha_Tocada(object? sender, TappedEventArgs e)
        {
            if (sender is BindableObject { BindingContext: LinhaTabela linha })
                LinhaTocada?.Invoke(this, linha);
        }
    }
}

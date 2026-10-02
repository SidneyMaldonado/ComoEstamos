using System.Globalization;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Controls
{
    /// <summary>Linha da tabela de contas, já formatada para exibição.</summary>
    public record ContaLinha(int Id, string Nome, string Saldo, Color CorSaldo, Color Fundo)
    {
        private static readonly CultureInfo PtBr = new("pt-BR");
        private static readonly Color CorSaldoPositivo = Color.FromArgb("#F2F4FA");
        private static readonly Color CorSaldoNegativo = Color.FromArgb("#FF9E9E");
        private static readonly Color FundoLinhaPar = Colors.Transparent;
        private static readonly Color FundoLinhaImpar = Color.FromArgb("#14FFFFFF");

        /// <summary>Monta as linhas na ordem recebida, alternando o fundo.</summary>
        public static List<ContaLinha> Montar(IEnumerable<Conta> contas) =>
            contas.Select((conta, i) => new ContaLinha(
                conta.Id,
                conta.Nome,
                conta.Saldo.ToString("C", PtBr),
                conta.Saldo < 0 ? CorSaldoNegativo : CorSaldoPositivo,
                i % 2 == 0 ? FundoLinhaPar : FundoLinhaImpar)).ToList();
    }

    /// <summary>Tabela de duas colunas (Conta e Saldo) usada na MainPage e na lista de contas.</summary>
    public partial class TabelaContas : ContentView
    {
        public static readonly BindableProperty ContasProperty = BindableProperty.Create(
            nameof(Contas), typeof(IReadOnlyList<ContaLinha>), typeof(TabelaContas),
            propertyChanged: (b, _, _) => ((TabelaContas)b).OnPropertyChanged(nameof(SemContas)));

        /// <summary>Linhas exibidas. Enquanto for nulo (ainda carregando), a mensagem de lista vazia não aparece.</summary>
        public IReadOnlyList<ContaLinha>? Contas
        {
            get => (IReadOnlyList<ContaLinha>?)GetValue(ContasProperty);
            set => SetValue(ContasProperty, value);
        }

        public bool SemContas => Contas is { Count: 0 };

        /// <summary>Disparado quando uma linha é tocada.</summary>
        public event EventHandler<ContaLinha>? ContaTocada;

        public TabelaContas()
        {
            InitializeComponent();
        }

        private void Linha_Tocada(object? sender, TappedEventArgs e)
        {
            if (sender is BindableObject { BindingContext: ContaLinha linha })
                ContaTocada?.Invoke(this, linha);
        }
    }
}

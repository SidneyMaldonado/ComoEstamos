using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Dividas
{
    /// <summary>
    /// Cálculo das parcelas de uma dívida, comum à inclusão e à alteração:
    /// valor de cada parcela, vencimento e descrição.
    /// </summary>
    internal static class Parcelamento
    {
        private const int TamanhoMaximoDescricao = 100;

        /// <summary>
        /// Valor de cada parcela, menos a última: o total dividido igualmente, truncado em 2 casas.
        /// A última recebe os centavos que sobrarem.
        /// </summary>
        public static decimal ValorDaParcela(decimal valorTotal, int numeroParcelas) =>
            numeroParcelas < 1 ? valorTotal : Math.Round(valorTotal / numeroParcelas, 2, MidpointRounding.ToZero);

        /// <summary>
        /// Vencimento da parcela de índice <paramref name="indice"/> (0 = primeira). A primeira vence em
        /// <see cref="Divida.DataPrimeiroVencimento"/>; as seguintes, nos meses subsequentes, no
        /// <see cref="Divida.DiaVencimento"/> (ou no último dia do mês, se ele for menor).
        /// </summary>
        public static DateTime Vencimento(Divida divida, int indice)
        {
            var inicio = divida.DataPrimeiroVencimento.Date;
            if (indice == 0)
                return inicio;
            var mes = inicio.AddMonths(indice);
            return new DateTime(mes.Year, mes.Month, Math.Min(divida.DiaVencimento, DateTime.DaysInMonth(mes.Year, mes.Month)));
        }

        /// <summary><c>"&lt;Nome&gt; &lt;n&gt;/&lt;total&gt;"</c>; o nome é cortado se passar de 100 caracteres.</summary>
        public static string Descricao(string nome, int numero, int total)
        {
            var sufixo = $" {numero}/{total}";
            var nomeCortado = nome.Trim();
            if (nomeCortado.Length + sufixo.Length > TamanhoMaximoDescricao)
                nomeCortado = nomeCortado[..(TamanhoMaximoDescricao - sufixo.Length)];
            return nomeCortado + sufixo;
        }

        /// <summary>
        /// Gera as parcelas de número <paramref name="primeiroNumero"/> até <see cref="Divida.NumeroParcelas"/>,
        /// dividindo <paramref name="valor"/> igualmente entre elas, com os centavos que sobrarem na última.
        /// Conta e categoria são as da dívida. Na inclusão, gera todas (a partir da 1ª) com o valor total.
        /// </summary>
        public static List<Parcela> Gerar(Divida divida, int primeiroNumero, decimal valor)
        {
            var total = divida.NumeroParcelas;
            var quantidade = total - primeiroNumero + 1;
            var valorParcela = ValorDaParcela(valor, quantidade);
            var parcelas = new List<Parcela>(quantidade);

            for (var numero = primeiroNumero; numero <= total; numero++)
            {
                parcelas.Add(new Parcela
                {
                    Divida = divida,
                    IdConta = divida.IdConta!.Value,
                    IdCategoria = divida.IdCategoria!.Value,
                    Descricao = Descricao(divida.Nome, numero, total),
                    Valor = numero == total ? valor - valorParcela * (quantidade - 1) : valorParcela,
                    DataVencimento = Vencimento(divida, numero - 1)
                });
            }

            return parcelas;
        }
    }
}

using System.Globalization;
using System.Text.RegularExpressions;

namespace ComoEstamos.Core.Formatacao
{
    /// <summary>Leitura e exibição de números e datas no formato brasileiro (1.250,50).</summary>
    public static partial class FormatoBr
    {
        public static readonly CultureInfo Cultura = new("pt-BR");

        /// <summary>
        /// Lê um número digitado. Aceita o formato brasileiro (<c>1.250,50</c>) e, sem vírgula,
        /// um único ponto como separador decimal (<c>1250.5</c>, <c>0.000123</c>), como sai de
        /// alguns teclados numéricos. A exceção é o ponto seguido de exatamente 3 dígitos com parte
        /// inteira diferente de zero, que é separador de milhar (<c>1.000</c> = mil).
        /// Também aceita "R$" e espaços.
        /// </summary>
        public static bool TentarLerDecimal(string? texto, out decimal valor)
        {
            valor = 0;
            if (string.IsNullOrWhiteSpace(texto))
                return false;

            texto = texto.Trim().Replace("R$", "").Replace(" ", "");
            if (!texto.Contains(','))
            {
                var ponto = texto.IndexOf('.');
                if (ponto >= 0 && texto.LastIndexOf('.') == ponto)
                {
                    var parteInteira = texto[..ponto].TrimStart('-', '+').Trim('0');
                    var ehMilhar = texto.Length - ponto - 1 == 3 && parteInteira.Length > 0;
                    if (!ehMilhar)
                        texto = texto.Replace('.', ',');
                }
            }
            // O TryParse aceita grupos de milhar de qualquer tamanho ("1.25,00" viraria 125); a regex exige grupos de 3.
            return NumeroBr().IsMatch(texto)
                && decimal.TryParse(texto, NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint, Cultura, out valor);
        }

        [GeneratedRegex(@"^[+-]?(\d+|\d{1,3}(\.\d{3})+)(,\d+)?$")]
        private static partial Regex NumeroBr();

        /// <summary>Valor em reais (<c>R$ 1.250,50</c>), com 2 casas e até <paramref name="casasMaximas"/> se precisar.</summary>
        public static string Moeda(decimal valor, int casasMaximas = 2)
        {
            var extras = new string('#', Math.Max(0, casasMaximas - 2));
            return valor.ToString($"R$ #,##0.00{extras};-R$ #,##0.00{extras}", Cultura);
        }

        /// <summary>Número com separador de milhar e só as casas decimais necessárias, até <paramref name="casasMaximas"/>.</summary>
        public static string Numero(decimal valor, int casasMaximas) =>
            valor.ToString(casasMaximas == 0 ? "#,##0" : "#,##0." + new string('#', casasMaximas), Cultura);

        /// <summary>Número com exatamente <paramref name="casas"/> casas decimais (<c>1.250,50</c>).</summary>
        public static string NumeroFixo(decimal valor, int casas) => valor.ToString("N" + casas, Cultura);

        /// <summary>Data (<c>07/10/2026</c>).</summary>
        public static string Data(DateTime data) => data.ToString("dd/MM/yyyy", Cultura);

        /// <summary>Mês por extenso e ano (<c>outubro/2026</c>).</summary>
        public static string MesAno(DateTime data) => data.ToString("MMMM/yyyy", Cultura);

        /// <summary>Data e hora (<c>07/10/2026 14:30</c>), ou "—" se nula.</summary>
        public static string DataHora(DateTime? data) => data?.ToString("dd/MM/yyyy HH:mm", Cultura) ?? "—";
    }
}

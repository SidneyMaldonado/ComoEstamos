using ComoEstamos.Core.Formatacao;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Pages.Parcelas
{
    /// <summary>
    /// Textos das parcelas que mudam com o tipo: na dívida, "Paga" e "Pagar"; na receita (crédito),
    /// "Recebida" e "Receber".
    /// </summary>
    internal static class TextosParcela
    {
        /// <summary>"dívida" ou "receita".</summary>
        public static string Tipo(bool ehDivida) => ehDivida ? "dívida" : "receita";

        /// <summary>"Paga em 05/10/2026" (ou "Recebida em"), "Vencida" ou "Em aberto".</summary>
        public static string Situacao(Parcela parcela, bool ehDivida) =>
            parcela.DataPagamento is DateTime pagamento ? $"{(ehDivida ? "Paga" : "Recebida")} em {FormatoBr.Data(pagamento)}"
            : parcela.DataVencimento.Date < DateTime.Today ? "Vencida"
            : "Em aberto";
    }
}

using ComoEstamos.Core.Formatacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Dividas
{
    /// <summary>Uma dívida (ou crédito) com parcela vencendo no mês do resumo.</summary>
    /// <param name="Valor">Soma das parcelas ativas que vencem no mês (em geral, uma só).</param>
    /// <param name="Vencimento">Vencimento da primeira parcela do mês.</param>
    /// <param name="Paga">Todas as parcelas do mês estão pagas.</param>
    /// <param name="DataPagamento">Último pagamento das parcelas do mês, quando todas estão pagas.</param>
    public record ItemResumoMes(int IdDivida, string Nome, decimal Valor, DateTime Vencimento, bool Paga, DateTime? DataPagamento);

    /// <summary>Resumo de um mês: dívidas (despesas) e créditos (receitas) com parcela vencendo nele.</summary>
    public record ResumoMes(DateTime Mes, IReadOnlyList<ItemResumoMes> Despesas, IReadOnlyList<ItemResumoMes> Receitas)
    {
        public decimal TotalDespesas => Despesas.Sum(i => i.Valor);

        public decimal TotalReceitas => Receitas.Sum(i => i.Valor);

        /// <summary>Receitas menos despesas; negativa quando as despesas são maiores.</summary>
        public decimal Diferenca => TotalReceitas - TotalDespesas;
    }

    public interface IBuscarResumoMesUseCase
    {
        /// <summary>
        /// Dívidas e créditos ativos do usuário com parcela ativa vencendo no mês de <paramref name="mes"/>
        /// (o dia é ignorado), cada um com a soma das parcelas do mês. Ordem: pelo vencimento; empate, por nome.
        /// </summary>
        Task<ResumoMes> ExecutarAsync(DateTime mes, int idUsuario);
    }

    public class BuscarResumoMesUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarResumoMesUseCase
    {
        public async Task<ResumoMes> ExecutarAsync(DateTime mes, int idUsuario)
        {
            var inicio = new DateTime(mes.Year, mes.Month, 1);
            var fim = inicio.AddMonths(1);

            await using var uow = await unitOfWorkFactory.IniciarAsync();
            var dividas = await uow.Dividas.ListarPorUsuarioAsync(idUsuario);
            var parcelasDoMes = (await uow.Parcelas.ListarPorUsuarioAsync(idUsuario))
                .Where(p => p.DataVencimento >= inicio && p.DataVencimento < fim)
                .ToLookup(p => p.IdDivida);

            var itens = dividas
                .Where(d => parcelasDoMes.Contains(d.Id))
                .Select(d => (divida: d, item: Resumir(d, parcelasDoMes[d.Id].ToList())))
                .OrderBy(i => i.item.Vencimento)
                .ThenBy(i => i.item.Nome, StringComparer.Create(FormatoBr.Cultura, ignoreCase: true))
                .ToList();

            return new ResumoMes(
                inicio,
                itens.Where(i => i.divida.EhDivida).Select(i => i.item).ToList(),
                itens.Where(i => !i.divida.EhDivida).Select(i => i.item).ToList());
        }

        private static ItemResumoMes Resumir(Divida divida, List<Parcela> parcelas)
        {
            var paga = parcelas.All(p => p.DataPagamento is not null);
            return new ItemResumoMes(
                divida.Id,
                divida.Nome,
                parcelas.Sum(p => p.Valor),
                parcelas.Min(p => p.DataVencimento),
                paga,
                paga ? parcelas.Max(p => p.DataPagamento) : null);
        }
    }
}

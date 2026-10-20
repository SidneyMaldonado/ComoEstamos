using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IOperacaoRepository : IRepository<Operacao>
    {
        Task<List<Operacao>> ListarPorInvestimentoAsync(int idInvestimento, bool incluirInativos = false);

        /// <summary>
        /// As <paramref name="quantidade"/> operações ativas mais recentes do investimento, da mais recente
        /// para a mais antiga (no empate de data, a gravada por último primeiro).
        /// </summary>
        Task<List<Operacao>> ListarUltimasPorInvestimentoAsync(int idInvestimento, int quantidade);

        Task<bool> ExisteAtivaPorInvestimentoAsync(int idInvestimento);
    }

    public class OperacaoRepository : RepositoryBase<Operacao>, IOperacaoRepository
    {
        public OperacaoRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

        internal OperacaoRepository(AppDbContext contexto) : base(contexto) { }

        public Task<List<Operacao>> ListarPorInvestimentoAsync(int idInvestimento, bool incluirInativos = false) =>
            ListarAsync(o => o.IdInvestimento == idInvestimento, incluirInativos, q => q.OrderByDescending(o => o.DataOperacao));

        public Task<List<Operacao>> ListarUltimasPorInvestimentoAsync(int idInvestimento, int quantidade) =>
            ListarAsync(o => o.IdInvestimento == idInvestimento, montarConsulta: q => q
                .OrderByDescending(o => o.DataOperacao)
                .ThenByDescending(o => o.Id)
                .Take(quantidade));

        public Task<bool> ExisteAtivaPorInvestimentoAsync(int idInvestimento) =>
            ExisteAtivoAsync(o => o.IdInvestimento == idInvestimento);
    }
}

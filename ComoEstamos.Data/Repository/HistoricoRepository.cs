using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IHistoricoRepository : IRepository<Historico>
    {
        Task<List<Historico>> ListarPorInvestimentoAsync(int idInvestimento, bool incluirInativos = false);
    }

    public class HistoricoRepository : RepositoryBase<Historico>, IHistoricoRepository
    {
        public HistoricoRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

        internal HistoricoRepository(AppDbContext contexto) : base(contexto) { }

        public Task<List<Historico>> ListarPorInvestimentoAsync(int idInvestimento, bool incluirInativos = false) =>
            ListarAsync(h => h.IdInvestimento == idInvestimento, incluirInativos, q => q.OrderByDescending(h => h.DataHistorico));
    }
}

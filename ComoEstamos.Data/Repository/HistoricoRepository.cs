using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IHistoricoRepository : IRepository<Historico>
    {
        Task<List<Historico>> ListarPorInvestimentoAsync(int idInvestimento, bool incluirInativos = false);
    }

    public class HistoricoRepository(IDbContextFactory<AppDbContext> contextFactory)
        : RepositoryBase<Historico>(contextFactory), IHistoricoRepository
    {
        public Task<List<Historico>> ListarPorInvestimentoAsync(int idInvestimento, bool incluirInativos = false) =>
            ListarAsync(h => h.IdInvestimento == idInvestimento, incluirInativos, q => q.OrderByDescending(h => h.DataHistorico));
    }
}

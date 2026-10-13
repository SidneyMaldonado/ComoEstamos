using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IHistoricoSaldoRepository : IRepository<HistoricoSaldo>
    {
        /// <summary>Históricos da conta, do mais recente para o mais antigo.</summary>
        Task<List<HistoricoSaldo>> ListarPorContaAsync(int idConta, bool incluirInativos = false);
    }

    public class HistoricoSaldoRepository : RepositoryBase<HistoricoSaldo>, IHistoricoSaldoRepository
    {
        public HistoricoSaldoRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

        internal HistoricoSaldoRepository(AppDbContext contexto) : base(contexto) { }

        public Task<List<HistoricoSaldo>> ListarPorContaAsync(int idConta, bool incluirInativos = false) =>
            ListarAsync(h => h.IdConta == idConta, incluirInativos, q => q.OrderByDescending(h => h.DataHistoricoSaldo));
    }
}

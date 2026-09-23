using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IInvestimentoRepository : IRepository<Investimento>
    {
        Task<List<Investimento>> ListarPorCarteiraAsync(int idCarteira, bool incluirInativos = false);
    }

    public class InvestimentoRepository(IDbContextFactory<AppDbContext> contextFactory)
        : RepositoryBase<Investimento>(contextFactory), IInvestimentoRepository
    {
        public Task<List<Investimento>> ListarPorCarteiraAsync(int idCarteira, bool incluirInativos = false) =>
            ListarAsync(i => i.IdCarteira == idCarteira, incluirInativos, q => q.OrderBy(i => i.Nome));
    }
}

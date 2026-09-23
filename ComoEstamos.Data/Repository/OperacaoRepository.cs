using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IOperacaoRepository : IRepository<Operacao>
    {
        Task<List<Operacao>> ListarPorInvestimentoAsync(int idInvestimento, bool incluirInativos = false);
    }

    public class OperacaoRepository(IDbContextFactory<AppDbContext> contextFactory)
        : RepositoryBase<Operacao>(contextFactory), IOperacaoRepository
    {
        public Task<List<Operacao>> ListarPorInvestimentoAsync(int idInvestimento, bool incluirInativos = false) =>
            ListarAsync(o => o.IdInvestimento == idInvestimento, incluirInativos, q => q.OrderByDescending(o => o.DataOperacao));
    }
}

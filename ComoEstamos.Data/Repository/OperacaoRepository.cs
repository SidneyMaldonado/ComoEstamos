using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IOperacaoRepository : IRepository<Operacao>
    {
        Task<List<Operacao>> ListarPorInvestimentoAsync(int idInvestimento, bool incluirInativos = false);
    }

    public class OperacaoRepository : RepositoryBase<Operacao>, IOperacaoRepository
    {
        public OperacaoRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

        internal OperacaoRepository(AppDbContext contexto) : base(contexto) { }

        public Task<List<Operacao>> ListarPorInvestimentoAsync(int idInvestimento, bool incluirInativos = false) =>
            ListarAsync(o => o.IdInvestimento == idInvestimento, incluirInativos, q => q.OrderByDescending(o => o.DataOperacao));
    }
}

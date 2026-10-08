using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IInvestimentoRepository : IRepository<Investimento>
    {
        Task<List<Investimento>> ListarPorCarteiraAsync(int idCarteira, bool incluirInativos = false);

        /// <summary>Investimentos das carteiras do usuário, com a Carteira carregada, por carteira e nome.</summary>
        Task<List<Investimento>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false);
        Task<bool> ExisteAtivoPorCarteiraAsync(int idCarteira);
    }

    public class InvestimentoRepository : RepositoryBase<Investimento>, IInvestimentoRepository
    {
        public InvestimentoRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

        internal InvestimentoRepository(AppDbContext contexto) : base(contexto) { }

        public Task<List<Investimento>> ListarPorCarteiraAsync(int idCarteira, bool incluirInativos = false) =>
            ListarAsync(i => i.IdCarteira == idCarteira, incluirInativos, q => q.OrderBy(i => i.Nome));

        public Task<List<Investimento>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false) =>
            ListarAsync(i => i.Carteira!.IdUsuario == idUsuario, incluirInativos, q => q
                .Include(i => i.Carteira)
                .OrderBy(i => i.Carteira!.Nome)
                .ThenBy(i => i.Nome));

        public Task<bool> ExisteAtivoPorCarteiraAsync(int idCarteira) => ExisteAtivoAsync(i => i.IdCarteira == idCarteira);
    }
}

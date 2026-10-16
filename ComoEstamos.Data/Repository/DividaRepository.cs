using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IDividaRepository : IRepository<Divida>
    {
        /// <summary>Lista as dívidas do usuário (dono da conta delas) com Credor, Conta e Categoria carregados.</summary>
        Task<List<Divida>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false);

        Task<bool> ExisteAtivaPorContaAsync(int idConta);
        Task<bool> ExisteAtivaPorCategoriaAsync(int idCategoria);
        Task<bool> ExisteAtivaPorCredorAsync(int idCredor);
    }

    public class DividaRepository : RepositoryBase<Divida>, IDividaRepository
    {
        public DividaRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

        internal DividaRepository(AppDbContext contexto) : base(contexto) { }

        public Task<List<Divida>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false) =>
            ListarAsync(d => d.Conta!.IdUsuario == idUsuario, incluirInativos, q => q
                .Include(d => d.Credor)
                .Include(d => d.Conta)
                .Include(d => d.Categoria)
                .OrderBy(d => d.Nome));

        public Task<bool> ExisteAtivaPorContaAsync(int idConta) => ExisteAtivoAsync(d => d.IdConta == idConta);

        public Task<bool> ExisteAtivaPorCategoriaAsync(int idCategoria) => ExisteAtivoAsync(d => d.IdCategoria == idCategoria);

        public Task<bool> ExisteAtivaPorCredorAsync(int idCredor) => ExisteAtivoAsync(d => d.IdCredor == idCredor);
    }
}

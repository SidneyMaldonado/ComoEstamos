using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IDividaRepository : IRepository<Divida>
    {
        /// <summary>Lista as dívidas do usuário com Credor, Conta e Categoria carregados.</summary>
        Task<List<Divida>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false);
    }

    public class DividaRepository(IDbContextFactory<AppDbContext> contextFactory)
        : RepositoryBase<Divida>(contextFactory), IDividaRepository
    {
        public Task<List<Divida>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false) =>
            ListarAsync(d => d.IdUsuario == idUsuario, incluirInativos, q => q
                .Include(d => d.Credor)
                .Include(d => d.Conta)
                .Include(d => d.Categoria)
                .OrderBy(d => d.Nome));
    }
}

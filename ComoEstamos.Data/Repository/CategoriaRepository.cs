using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface ICategoriaRepository : IRepository<Categoria>
    {
        Task<List<Categoria>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false);
    }

    public class CategoriaRepository(IDbContextFactory<AppDbContext> contextFactory)
        : RepositoryBase<Categoria>(contextFactory), ICategoriaRepository
    {
        public Task<List<Categoria>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false) =>
            ListarAsync(c => c.IdUsuario == idUsuario, incluirInativos, q => q.OrderBy(c => c.Nome));
    }
}

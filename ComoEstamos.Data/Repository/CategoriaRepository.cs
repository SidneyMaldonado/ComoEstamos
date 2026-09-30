using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface ICategoriaRepository : IRepository<Categoria>
    {
        Task<List<Categoria>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false);
    }

    public class CategoriaRepository : RepositoryBase<Categoria>, ICategoriaRepository
    {
        public CategoriaRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

        internal CategoriaRepository(AppDbContext contexto) : base(contexto) { }

        public Task<List<Categoria>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false) =>
            ListarAsync(c => c.IdUsuario == idUsuario, incluirInativos, q => q.OrderBy(c => c.Nome));
    }
}

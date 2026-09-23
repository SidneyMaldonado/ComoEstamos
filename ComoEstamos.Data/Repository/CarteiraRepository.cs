using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface ICarteiraRepository : IRepository<Carteira>
    {
        Task<List<Carteira>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false);
    }

    public class CarteiraRepository(IDbContextFactory<AppDbContext> contextFactory)
        : RepositoryBase<Carteira>(contextFactory), ICarteiraRepository
    {
        public Task<List<Carteira>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false) =>
            ListarAsync(c => c.IdUsuario == idUsuario, incluirInativos, q => q.OrderBy(c => c.Nome));
    }
}

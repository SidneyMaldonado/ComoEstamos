using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface ICredorRepository : IRepository<Credor>
    {
        Task<List<Credor>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false);
    }

    public class CredorRepository : RepositoryBase<Credor>, ICredorRepository
    {
        public CredorRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

        internal CredorRepository(AppDbContext contexto) : base(contexto) { }

        public Task<List<Credor>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false) =>
            ListarAsync(c => c.IdUsuario == idUsuario, incluirInativos, q => q.OrderBy(c => c.Nome));
    }
}

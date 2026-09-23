using System.Linq.Expressions;
using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IRepository<T> where T : EntidadeBase
    {
        Task<T?> ObterPorIdAsync(int id);
        Task<List<T>> ListarAsync(bool incluirInativos = false);
        Task<T> InserirAsync(T entidade);
        Task AtualizarAsync(T entidade);
        Task DesativarAsync(int id);
        Task ReativarAsync(int id);
    }

    /// <summary>
    /// Operações comuns a todos os repositórios. Cada operação abre e fecha
    /// seu próprio DbContext, e as entidades retornadas não ficam rastreadas.
    /// </summary>
    public abstract class RepositoryBase<T>(IDbContextFactory<AppDbContext> contextFactory) : IRepository<T>
        where T : EntidadeBase
    {
        protected readonly IDbContextFactory<AppDbContext> ContextFactory = contextFactory;

        public async Task<T?> ObterPorIdAsync(int id)
        {
            await using var db = await ContextFactory.CreateDbContextAsync();
            var entidade = await db.Set<T>().FindAsync(id);
            if (entidade is not null)
                db.Entry(entidade).State = EntityState.Detached;
            return entidade;
        }

        public Task<List<T>> ListarAsync(bool incluirInativos = false) =>
            ListarAsync(_ => true, incluirInativos);

        public async Task<T> InserirAsync(T entidade)
        {
            await using var db = await ContextFactory.CreateDbContextAsync();
            // Só a própria entidade: navegações preenchidas não são inseridas junto.
            db.Entry(entidade).State = EntityState.Added;
            await db.SaveChangesAsync();
            return entidade;
        }

        public async Task AtualizarAsync(T entidade)
        {
            await using var db = await ContextFactory.CreateDbContextAsync();
            db.Entry(entidade).State = EntityState.Modified;
            await db.SaveChangesAsync();
        }

        public Task DesativarAsync(int id) => DefinirAtivoAsync(id, false);

        public Task ReativarAsync(int id) => DefinirAtivoAsync(id, true);

        protected async Task<List<T>> ListarAsync(
            Expression<Func<T, bool>> filtro,
            bool incluirInativos = false,
            Func<IQueryable<T>, IQueryable<T>>? montarConsulta = null)
        {
            await using var db = await ContextFactory.CreateDbContextAsync();
            var consulta = db.Set<T>().AsNoTracking().Where(filtro);
            if (!incluirInativos)
                consulta = consulta.Where(e => e.Ativo);
            if (montarConsulta is not null)
                consulta = montarConsulta(consulta);
            return await consulta.ToListAsync();
        }

        private async Task DefinirAtivoAsync(int id, bool ativo)
        {
            await using var db = await ContextFactory.CreateDbContextAsync();
            var entidade = await db.Set<T>().FindAsync(id)
                ?? throw new KeyNotFoundException($"{typeof(T).Name} {id} não encontrado.");
            entidade.Ativo = ativo;
            await db.SaveChangesAsync();
        }
    }
}

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
    /// Operações comuns a todos os repositórios. Funciona de dois modos:
    /// <list type="bullet">
    /// <item><b>Avulso</b> (criado com a factory): cada operação abre, salva e fecha
    /// seu próprio DbContext, e as entidades retornadas não ficam rastreadas.</item>
    /// <item><b>Dentro de uma <see cref="IUnitOfWork"/></b>: todas as operações usam o
    /// DbContext da unidade, nada é salvo até <see cref="IUnitOfWork.ConfirmarAsync"/>,
    /// e as entidades obtidas por Id ficam rastreadas por ela.</item>
    /// </list>
    /// </summary>
    public abstract class RepositoryBase<T> : IRepository<T>
        where T : EntidadeBase
    {
        private readonly IDbContextFactory<AppDbContext>? _contextFactory;
        private readonly AppDbContext? _contextoCompartilhado;

        protected RepositoryBase(IDbContextFactory<AppDbContext> contextFactory) =>
            _contextFactory = contextFactory;

        private protected RepositoryBase(AppDbContext contextoCompartilhado) =>
            _contextoCompartilhado = contextoCompartilhado;

        public async Task<T?> ObterPorIdAsync(int id)
        {
            await using var ctx = await AbrirContextoAsync();
            var entidade = await ctx.Db.Set<T>().FindAsync(id);
            // Na unidade de trabalho a entidade continua rastreada, para não perder alterações pendentes.
            if (entidade is not null && !ctx.Compartilhado)
                ctx.Db.Entry(entidade).State = EntityState.Detached;
            return entidade;
        }

        public Task<List<T>> ListarAsync(bool incluirInativos = false) =>
            ListarAsync(_ => true, incluirInativos);

        public async Task<T> InserirAsync(T entidade)
        {
            await using var ctx = await AbrirContextoAsync();
            // Só a própria entidade: navegações preenchidas não são inseridas junto.
            ctx.Db.Entry(entidade).State = EntityState.Added;
            await ctx.SalvarAsync();
            return entidade;
        }

        public async Task AtualizarAsync(T entidade)
        {
            await using var ctx = await AbrirContextoAsync();
            ctx.Db.Entry(entidade).State = EntityState.Modified;
            await ctx.SalvarAsync();
        }

        public Task DesativarAsync(int id) => DefinirAtivoAsync(id, false);

        public Task ReativarAsync(int id) => DefinirAtivoAsync(id, true);

        /// <summary>
        /// Contexto para uma operação. Use sempre com <c>await using</c> e grave
        /// com <see cref="ContextoEmUso.SalvarAsync"/>, nunca com <c>Db.SaveChangesAsync</c>.
        /// </summary>
        protected async Task<ContextoEmUso> AbrirContextoAsync() =>
            _contextoCompartilhado is not null
                ? new ContextoEmUso(_contextoCompartilhado, compartilhado: true)
                : new ContextoEmUso(await _contextFactory!.CreateDbContextAsync(), compartilhado: false);

        protected async Task<List<T>> ListarAsync(
            Expression<Func<T, bool>> filtro,
            bool incluirInativos = false,
            Func<IQueryable<T>, IQueryable<T>>? montarConsulta = null)
        {
            await using var ctx = await AbrirContextoAsync();
            var consulta = ctx.Db.Set<T>().AsNoTracking().Where(filtro);
            if (!incluirInativos)
                consulta = consulta.Where(e => e.Ativo);
            if (montarConsulta is not null)
                consulta = montarConsulta(consulta);
            return await consulta.ToListAsync();
        }

        private async Task DefinirAtivoAsync(int id, bool ativo)
        {
            await using var ctx = await AbrirContextoAsync();
            var entidade = await ctx.Db.Set<T>().FindAsync(id)
                ?? throw new KeyNotFoundException($"{typeof(T).Name} {id} não encontrado.");
            entidade.Ativo = ativo;
            await ctx.SalvarAsync();
        }

        /// <summary>
        /// DbContext em uso por uma operação. Avulso: salvar grava e o descarte fecha o contexto.
        /// Compartilhado (unidade de trabalho): salvar e descartar não fazem nada — quem grava é a unidade.
        /// </summary>
        protected sealed class ContextoEmUso(AppDbContext db, bool compartilhado) : IAsyncDisposable
        {
            public AppDbContext Db { get; } = db;

            public bool Compartilhado { get; } = compartilhado;

            public Task SalvarAsync() => Compartilhado ? Task.CompletedTask : Db.SaveChangesAsync();

            public ValueTask DisposeAsync() => Compartilhado ? ValueTask.CompletedTask : Db.DisposeAsync();
        }
    }
}

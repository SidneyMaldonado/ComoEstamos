using ComoEstamos.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data
{
    /// <summary>
    /// Agrupa operações de vários repositórios em uma única gravação atômica.
    /// Todos os repositórios expostos compartilham o mesmo DbContext; nada é gravado
    /// até <see cref="ConfirmarAsync"/>. Descartar sem confirmar desfaz tudo.
    /// </summary>
    /// <remarks>
    /// Consultas de listagem (ListarAsync, ListarPor...) leem apenas o que já está
    /// gravado no banco; ObterPorIdAsync enxerga também as alterações pendentes.
    /// Entidades inseridas só recebem Id depois de <see cref="ConfirmarAsync"/> —
    /// para relacioná-las antes disso, use a propriedade de navegação.
    /// </remarks>
    public interface IUnitOfWork : IAsyncDisposable
    {
        IUsuarioRepository Usuarios { get; }
        ICarteiraRepository Carteiras { get; }
        ICategoriaRepository Categorias { get; }
        IContaRepository Contas { get; }
        ICredorRepository Credores { get; }
        IDividaRepository Dividas { get; }
        IParcelaRepository Parcelas { get; }
        IInvestimentoRepository Investimentos { get; }
        IOperacaoRepository Operacoes { get; }
        IHistoricoRepository Historicos { get; }

        /// <summary>Grava todas as alterações pendentes de uma só vez (tudo ou nada).</summary>
        Task ConfirmarAsync();
    }

    public interface IUnitOfWorkFactory
    {
        /// <summary>Abre uma nova unidade de trabalho. Use com <c>await using</c>.</summary>
        Task<IUnitOfWork> IniciarAsync();
    }

    public class UnitOfWorkFactory(IDbContextFactory<AppDbContext> contextFactory) : IUnitOfWorkFactory
    {
        public async Task<IUnitOfWork> IniciarAsync() =>
            new UnitOfWork(await contextFactory.CreateDbContextAsync());
    }

    internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
    {
        private IUsuarioRepository? _usuarios;
        private ICarteiraRepository? _carteiras;
        private ICategoriaRepository? _categorias;
        private IContaRepository? _contas;
        private ICredorRepository? _credores;
        private IDividaRepository? _dividas;
        private IParcelaRepository? _parcelas;
        private IInvestimentoRepository? _investimentos;
        private IOperacaoRepository? _operacoes;
        private IHistoricoRepository? _historicos;

        public IUsuarioRepository Usuarios => _usuarios ??= new UsuarioRepository(db);
        public ICarteiraRepository Carteiras => _carteiras ??= new CarteiraRepository(db);
        public ICategoriaRepository Categorias => _categorias ??= new CategoriaRepository(db);
        public IContaRepository Contas => _contas ??= new ContaRepository(db);
        public ICredorRepository Credores => _credores ??= new CredorRepository(db);
        public IDividaRepository Dividas => _dividas ??= new DividaRepository(db);
        public IParcelaRepository Parcelas => _parcelas ??= new ParcelaRepository(db);
        public IInvestimentoRepository Investimentos => _investimentos ??= new InvestimentoRepository(db);
        public IOperacaoRepository Operacoes => _operacoes ??= new OperacaoRepository(db);
        public IHistoricoRepository Historicos => _historicos ??= new HistoricoRepository(db);

        public Task ConfirmarAsync() => db.SaveChangesAsync();

        public ValueTask DisposeAsync() => db.DisposeAsync();
    }
}

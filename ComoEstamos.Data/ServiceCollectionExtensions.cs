using ComoEstamos.Data.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ComoEstamos.Data
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>Registra o AppDbContext (SQLite no caminho informado) e todos os repositórios.</summary>
        public static IServiceCollection AddComoEstamosData(this IServiceCollection services, string caminhoBanco)
        {
            services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite($"Data Source={caminhoBanco}"));

            services.AddSingleton<IUsuarioRepository, UsuarioRepository>();
            services.AddSingleton<ICarteiraRepository, CarteiraRepository>();
            services.AddSingleton<ICategoriaRepository, CategoriaRepository>();
            services.AddSingleton<IContaRepository, ContaRepository>();
            services.AddSingleton<ICredorRepository, CredorRepository>();
            services.AddSingleton<IDividaRepository, DividaRepository>();
            services.AddSingleton<IParcelaRepository, ParcelaRepository>();
            services.AddSingleton<IInvestimentoRepository, InvestimentoRepository>();
            services.AddSingleton<IOperacaoRepository, OperacaoRepository>();
            services.AddSingleton<IHistoricoRepository, HistoricoRepository>();

            return services;
        }

        /// <summary>Cria o banco, se não existir, e aplica as migrations pendentes.</summary>
        public static void AplicarMigrations(this IServiceProvider services)
        {
            var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = factory.CreateDbContext();
            db.Database.Migrate();
        }
    }
}

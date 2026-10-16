using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ComoEstamos.Test.RepositoryTests
{
    /// <summary>
    /// Base dos testes de repositório: cada teste recebe um banco InMemory próprio,
    /// isolado dos demais (os testes rodam em paralelo por método).
    /// </summary>
    public abstract class RepositoryTestBase
    {
        protected IDbContextFactory<AppDbContext> ContextFactory { get; private set; } = null!;

        [TestInitialize]
        public void CriarBanco()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"ComoEstamos_{Guid.NewGuid()}")
                .Options;
            ContextFactory = new PooledDbContextFactory<AppDbContext>(options);
        }

        protected AppDbContext CriarContexto() => ContextFactory.CreateDbContext();

        /// <summary>Grava as entidades direto no banco, sem passar pelo repositório testado.</summary>
        protected async Task SemearAsync(params object[] entidades)
        {
            await using var db = CriarContexto();
            db.AddRange(entidades);
            await db.SaveChangesAsync();
        }

        protected async Task<Usuario> CriarUsuarioAsync(string email = "usuario@teste.com")
        {
            var usuario = new Usuario { Nome = "Usuário Teste", Email = email, SenhaHash = "hash" };
            await SemearAsync(usuario);
            return usuario;
        }

        /// <summary>Conta de um outro usuário. A dívida não tem id_usuario: o dono é o da conta dela.</summary>
        protected async Task<Conta> CriarContaDeOutroUsuarioAsync(string email = "outro@teste.com")
        {
            var outro = await CriarUsuarioAsync(email);
            var conta = new Conta { IdUsuario = outro.Id, Nome = "Do outro" };
            await SemearAsync(conta);
            return conta;
        }
    }
}

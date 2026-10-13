using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases.Credores;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.ServiceTests
{
    /// <summary>O service repassa cada operação ao caso de uso com o usuário atual.</summary>
    [TestClass]
    public sealed class CredorServiceTests : RepositoryTestBase
    {
        private CredorService _service = null!;
        private UsuarioAtual _usuarioAtual = null!;

        [TestInitialize]
        public void Inicializar()
        {
            var uow = new UnitOfWorkFactory(ContextFactory);
            _usuarioAtual = new UsuarioAtual(new UsuarioRepository(ContextFactory));
            _service = new CredorService(
                _usuarioAtual,
                new BuscarTodosCredoresUseCase(uow),
                new BuscarCredorPorIdUseCase(uow),
                new IncluirCredorUseCase(uow),
                new AlterarCredorUseCase(uow),
                new ExcluirCredorUseCase(uow));
        }

        [TestMethod]
        public async Task IncluirAsync_GravaParaOUsuarioAtual()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");

            await _service.IncluirAsync(new Credor { IdUsuario = outro.Id, Nome = "Banco" });

            await using var db = CriarContexto();
            Assert.AreEqual(await _usuarioAtual.ObterIdAsync(), (await db.Credores.SingleAsync()).IdUsuario);
        }

        [TestMethod]
        public async Task BuscarTodosAsync_RetornaSoOsDoUsuarioAtual()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            await SemearAsync(new Credor { IdUsuario = outro.Id, Nome = "Loja" });
            await _service.IncluirAsync(new Credor { Nome = "Banco" });

            var lista = await _service.BuscarTodosAsync();

            Assert.AreEqual("Banco", lista.Single().Nome);
        }

        [TestMethod]
        public async Task BuscarAlterarEExcluir_UsamOUsuarioAtual()
        {
            var credor = await _service.IncluirAsync(new Credor { Nome = "Banco" });

            await _service.AlterarAsync(new Credor { Id = credor.Id, Nome = "Loja" });
            Assert.AreEqual("Loja", (await _service.BuscarPorIdAsync(credor.Id))?.Nome);

            await _service.ExcluirAsync(credor.Id);
            Assert.IsNull(await _service.BuscarPorIdAsync(credor.Id));
        }

        [TestMethod]
        public async Task BuscarPorIdAsync_NaoRetornaRegistroDeOutroUsuario()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var alheio = new Credor { IdUsuario = outro.Id, Nome = "Loja" };
            await SemearAsync(alheio);

            Assert.IsNull(await _service.BuscarPorIdAsync(alheio.Id));
        }
    }
}

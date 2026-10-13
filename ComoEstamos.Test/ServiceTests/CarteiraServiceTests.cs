using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases.Carteiras;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.ServiceTests
{
    /// <summary>O service repassa cada operação ao caso de uso com o usuário atual.</summary>
    [TestClass]
    public sealed class CarteiraServiceTests : RepositoryTestBase
    {
        private CarteiraService _service = null!;
        private UsuarioAtual _usuarioAtual = null!;

        [TestInitialize]
        public void Inicializar()
        {
            var uow = new UnitOfWorkFactory(ContextFactory);
            _usuarioAtual = new UsuarioAtual(new UsuarioRepository(ContextFactory));
            _service = new CarteiraService(
                _usuarioAtual,
                new BuscarTodasCarteirasUseCase(uow),
                new BuscarCarteiraPorIdUseCase(uow),
                new IncluirCarteiraUseCase(uow),
                new AlterarCarteiraUseCase(uow),
                new ExcluirCarteiraUseCase(uow));
        }

        [TestMethod]
        public async Task IncluirAsync_GravaParaOUsuarioAtual()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");

            await _service.IncluirAsync(new Carteira { IdUsuario = outro.Id, Nome = "Ações" });

            await using var db = CriarContexto();
            Assert.AreEqual(await _usuarioAtual.ObterIdAsync(), (await db.Carteiras.SingleAsync()).IdUsuario);
        }

        [TestMethod]
        public async Task BuscarTodasAsync_RetornaSoOsDoUsuarioAtual()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            await SemearAsync(new Carteira { IdUsuario = outro.Id, Nome = "Renda Fixa" });
            await _service.IncluirAsync(new Carteira { Nome = "Ações" });

            var lista = await _service.BuscarTodasAsync();

            Assert.AreEqual("Ações", lista.Single().Nome);
        }

        [TestMethod]
        public async Task BuscarAlterarEExcluir_UsamOUsuarioAtual()
        {
            var carteira = await _service.IncluirAsync(new Carteira { Nome = "Ações" });

            await _service.AlterarAsync(new Carteira { Id = carteira.Id, Nome = "Renda Fixa" });
            Assert.AreEqual("Renda Fixa", (await _service.BuscarPorIdAsync(carteira.Id))?.Nome);

            await _service.ExcluirAsync(carteira.Id);
            Assert.IsNull(await _service.BuscarPorIdAsync(carteira.Id));
        }

        [TestMethod]
        public async Task BuscarPorIdAsync_NaoRetornaRegistroDeOutroUsuario()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var alheio = new Carteira { IdUsuario = outro.Id, Nome = "Renda Fixa" };
            await SemearAsync(alheio);

            Assert.IsNull(await _service.BuscarPorIdAsync(alheio.Id));
        }
    }
}

using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases.Contas;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.ServiceTests
{
    /// <summary>O service repassa cada operação ao caso de uso com o usuário atual.</summary>
    [TestClass]
    public sealed class ContaServiceTests : RepositoryTestBase
    {
        private ContaService _service = null!;
        private UsuarioAtual _usuarioAtual = null!;

        [TestInitialize]
        public void Inicializar()
        {
            var uow = new UnitOfWorkFactory(ContextFactory);
            _usuarioAtual = new UsuarioAtual(new UsuarioRepository(ContextFactory));
            _service = new ContaService(
                _usuarioAtual,
                new BuscarTodasContasUseCase(uow),
                new BuscarContaPorIdUseCase(uow),
                new IncluirContaUseCase(uow),
                new AlterarContaUseCase(uow),
                new ExcluirContaUseCase(uow));
        }

        [TestMethod]
        public async Task IncluirAsync_GravaParaOUsuarioAtual()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");

            await _service.IncluirAsync(new Conta { IdUsuario = outro.Id, Nome = "Nubank" });

            await using var db = CriarContexto();
            Assert.AreEqual(await _usuarioAtual.ObterIdAsync(), (await db.Contas.SingleAsync()).IdUsuario);
        }

        [TestMethod]
        public async Task BuscarTodasAsync_RetornaSoOsDoUsuarioAtual()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            await SemearAsync(new Conta { IdUsuario = outro.Id, Nome = "Inter" });
            await _service.IncluirAsync(new Conta { Nome = "Nubank" });

            var lista = await _service.BuscarTodasAsync();

            Assert.AreEqual("Nubank", lista.Single().Nome);
        }

        [TestMethod]
        public async Task BuscarAlterarEExcluir_UsamOUsuarioAtual()
        {
            var conta = await _service.IncluirAsync(new Conta { Nome = "Nubank" });

            await _service.AlterarAsync(new Conta { Id = conta.Id, Nome = "Inter" });
            Assert.AreEqual("Inter", (await _service.BuscarPorIdAsync(conta.Id))?.Nome);

            await _service.ExcluirAsync(conta.Id);
            Assert.IsNull(await _service.BuscarPorIdAsync(conta.Id));
        }

        [TestMethod]
        public async Task BuscarPorIdAsync_NaoRetornaRegistroDeOutroUsuario()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var alheio = new Conta { IdUsuario = outro.Id, Nome = "Inter" };
            await SemearAsync(alheio);

            Assert.IsNull(await _service.BuscarPorIdAsync(alheio.Id));
        }
    }
}

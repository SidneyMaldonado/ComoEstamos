using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases.Categorias;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.ServiceTests
{
    /// <summary>O service repassa cada operação ao caso de uso com o usuário atual.</summary>
    [TestClass]
    public sealed class CategoriaServiceTests : RepositoryTestBase
    {
        private CategoriaService _service = null!;
        private UsuarioAtual _usuarioAtual = null!;

        [TestInitialize]
        public void Inicializar()
        {
            var uow = new UnitOfWorkFactory(ContextFactory);
            _usuarioAtual = new UsuarioAtual(new UsuarioRepository(ContextFactory));
            _service = new CategoriaService(
                _usuarioAtual,
                new BuscarTodasCategoriasUseCase(uow),
                new BuscarCategoriaPorIdUseCase(uow),
                new IncluirCategoriaUseCase(uow),
                new AlterarCategoriaUseCase(uow),
                new ExcluirCategoriaUseCase(uow));
        }

        [TestMethod]
        public async Task IncluirAsync_GravaParaOUsuarioAtual()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");

            await _service.IncluirAsync(new Categoria { IdUsuario = outro.Id, Nome = "Moradia" });

            await using var db = CriarContexto();
            Assert.AreEqual(await _usuarioAtual.ObterIdAsync(), (await db.Categorias.SingleAsync()).IdUsuario);
        }

        [TestMethod]
        public async Task BuscarTodasAsync_RetornaSoOsDoUsuarioAtual()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            await SemearAsync(new Categoria { IdUsuario = outro.Id, Nome = "Lazer" });
            await _service.IncluirAsync(new Categoria { Nome = "Moradia" });

            var lista = await _service.BuscarTodasAsync();

            Assert.AreEqual("Moradia", lista.Single().Nome);
        }

        [TestMethod]
        public async Task BuscarAlterarEExcluir_UsamOUsuarioAtual()
        {
            var categoria = await _service.IncluirAsync(new Categoria { Nome = "Moradia" });

            await _service.AlterarAsync(new Categoria { Id = categoria.Id, Nome = "Lazer" });
            Assert.AreEqual("Lazer", (await _service.BuscarPorIdAsync(categoria.Id))?.Nome);

            await _service.ExcluirAsync(categoria.Id);
            Assert.IsNull(await _service.BuscarPorIdAsync(categoria.Id));
        }

        [TestMethod]
        public async Task BuscarPorIdAsync_NaoRetornaRegistroDeOutroUsuario()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var alheio = new Categoria { IdUsuario = outro.Id, Nome = "Lazer" };
            await SemearAsync(alheio);

            Assert.IsNull(await _service.BuscarPorIdAsync(alheio.Id));
        }
    }
}

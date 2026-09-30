using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;

namespace ComoEstamos.Test.RepositoryTests
{
    [TestClass]
    public sealed class CategoriaRepositoryTests : RepositoryTestBase
    {
        private CategoriaRepository _repository = null!;

        [TestInitialize]
        public void Inicializar() => _repository = new CategoriaRepository(ContextFactory);

        [TestMethod]
        public async Task InserirAsync_PersisteCategoriaComImagem()
        {
            var usuario = await CriarUsuarioAsync();
            byte[] imagem = [1, 2, 3];

            var categoria = await _repository.InserirAsync(
                new Categoria { IdUsuario = usuario.Id, Nome = "Mercado", Imagem = imagem });

            var obtida = await _repository.ObterPorIdAsync(categoria.Id);
            Assert.IsNotNull(obtida);
            Assert.AreEqual("Mercado", obtida.Nome);
            CollectionAssert.AreEqual(imagem, obtida.Imagem);
        }

        [TestMethod]
        public async Task InserirAsync_PermiteImagemNula()
        {
            var usuario = await CriarUsuarioAsync();

            var categoria = await _repository.InserirAsync(new Categoria { IdUsuario = usuario.Id, Nome = "Lazer" });

            var obtida = await _repository.ObterPorIdAsync(categoria.Id);
            Assert.IsNotNull(obtida);
            Assert.IsNull(obtida.Imagem);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_RetornaSomenteCategoriasDoUsuario()
        {
            var usuario = await CriarUsuarioAsync("a@teste.com");
            var outro = await CriarUsuarioAsync("b@teste.com");
            await SemearAsync(
                new Categoria { IdUsuario = usuario.Id, Nome = "Minha" },
                new Categoria { IdUsuario = outro.Id, Nome = "De outro" });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual("Minha", lista[0].Nome);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_OrdenaPorNome()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(
                new Categoria { IdUsuario = usuario.Id, Nome = "Transporte" },
                new Categoria { IdUsuario = usuario.Id, Nome = "Alimentação" },
                new Categoria { IdUsuario = usuario.Id, Nome = "Moradia" });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            CollectionAssert.AreEqual(new[] { "Alimentação", "Moradia", "Transporte" }, lista.Select(c => c.Nome).ToArray());
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_IgnoraInativosPorPadrao()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(
                new Categoria { IdUsuario = usuario.Id, Nome = "Ativa" },
                new Categoria { IdUsuario = usuario.Id, Nome = "Inativa", Ativo = false });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual("Ativa", lista[0].Nome);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_IncluiInativosQuandoSolicitado()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(
                new Categoria { IdUsuario = usuario.Id, Nome = "Ativa" },
                new Categoria { IdUsuario = usuario.Id, Nome = "Inativa", Ativo = false });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id, incluirInativos: true);

            Assert.HasCount(2, lista);
        }

        [TestMethod]
        public async Task AtualizarAsync_AlteraNomeEImagem()
        {
            var usuario = await CriarUsuarioAsync();
            var categoria = await _repository.InserirAsync(new Categoria { IdUsuario = usuario.Id, Nome = "Mercado" });

            categoria.Nome = "Supermercado";
            categoria.Imagem = [9, 9];
            await _repository.AtualizarAsync(categoria);

            var obtida = await _repository.ObterPorIdAsync(categoria.Id);
            Assert.IsNotNull(obtida);
            Assert.AreEqual("Supermercado", obtida.Nome);
            CollectionAssert.AreEqual(new byte[] { 9, 9 }, obtida.Imagem);
        }

        [TestMethod]
        public async Task DesativarEReativarAsync_AlternaPresencaNaListagem()
        {
            var usuario = await CriarUsuarioAsync();
            var categoria = await _repository.InserirAsync(new Categoria { IdUsuario = usuario.Id, Nome = "Mercado" });

            await _repository.DesativarAsync(categoria.Id);
            Assert.IsEmpty(await _repository.ListarPorUsuarioAsync(usuario.Id));

            await _repository.ReativarAsync(categoria.Id);
            Assert.HasCount(1, await _repository.ListarPorUsuarioAsync(usuario.Id));
        }
    }
}

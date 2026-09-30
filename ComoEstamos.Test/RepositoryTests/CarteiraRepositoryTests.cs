using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;

namespace ComoEstamos.Test.RepositoryTests
{
    [TestClass]
    public sealed class CarteiraRepositoryTests : RepositoryTestBase
    {
        private CarteiraRepository _repository = null!;

        [TestInitialize]
        public void Inicializar() => _repository = new CarteiraRepository(ContextFactory);

        [TestMethod]
        public async Task InserirAsync_PersisteCarteira()
        {
            var usuario = await CriarUsuarioAsync();

            var carteira = await _repository.InserirAsync(new Carteira { IdUsuario = usuario.Id, Nome = "Ações" });

            var obtida = await _repository.ObterPorIdAsync(carteira.Id);
            Assert.IsNotNull(obtida);
            Assert.AreEqual("Ações", obtida.Nome);
            Assert.AreEqual(usuario.Id, obtida.IdUsuario);
            Assert.IsTrue(obtida.Ativo);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_RetornaSomenteCarteirasDoUsuario()
        {
            var usuario = await CriarUsuarioAsync("a@teste.com");
            var outro = await CriarUsuarioAsync("b@teste.com");
            await SemearAsync(
                new Carteira { IdUsuario = usuario.Id, Nome = "Minha" },
                new Carteira { IdUsuario = outro.Id, Nome = "De outro" });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual("Minha", lista[0].Nome);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_OrdenaPorNome()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(
                new Carteira { IdUsuario = usuario.Id, Nome = "Renda Fixa" },
                new Carteira { IdUsuario = usuario.Id, Nome = "Ações" },
                new Carteira { IdUsuario = usuario.Id, Nome = "Fundos" });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            CollectionAssert.AreEqual(new[] { "Ações", "Fundos", "Renda Fixa" }, lista.Select(c => c.Nome).ToArray());
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_IgnoraInativosPorPadrao()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(
                new Carteira { IdUsuario = usuario.Id, Nome = "Ativa" },
                new Carteira { IdUsuario = usuario.Id, Nome = "Inativa", Ativo = false });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual("Ativa", lista[0].Nome);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_IncluiInativosQuandoSolicitado()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(
                new Carteira { IdUsuario = usuario.Id, Nome = "Ativa" },
                new Carteira { IdUsuario = usuario.Id, Nome = "Inativa", Ativo = false });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id, incluirInativos: true);

            Assert.HasCount(2, lista);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_RetornaVazioParaUsuarioSemCarteiras()
        {
            var usuario = await CriarUsuarioAsync();

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            Assert.IsEmpty(lista);
        }

        [TestMethod]
        public async Task AtualizarAsync_AlteraNome()
        {
            var usuario = await CriarUsuarioAsync();
            var carteira = await _repository.InserirAsync(new Carteira { IdUsuario = usuario.Id, Nome = "Ações" });

            carteira.Nome = "Ações BR";
            await _repository.AtualizarAsync(carteira);

            var obtida = await _repository.ObterPorIdAsync(carteira.Id);
            Assert.AreEqual("Ações BR", obtida?.Nome);
        }

        [TestMethod]
        public async Task DesativarAsync_RemoveDaListagemDoUsuario()
        {
            var usuario = await CriarUsuarioAsync();
            var carteira = await _repository.InserirAsync(new Carteira { IdUsuario = usuario.Id, Nome = "Ações" });

            await _repository.DesativarAsync(carteira.Id);

            Assert.IsEmpty(await _repository.ListarPorUsuarioAsync(usuario.Id));
            Assert.HasCount(1, await _repository.ListarPorUsuarioAsync(usuario.Id, incluirInativos: true));
        }
    }
}

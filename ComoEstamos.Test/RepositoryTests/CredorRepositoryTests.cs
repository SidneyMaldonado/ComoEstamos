using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;

namespace ComoEstamos.Test.RepositoryTests
{
    [TestClass]
    public sealed class CredorRepositoryTests : RepositoryTestBase
    {
        private CredorRepository _repository = null!;

        [TestInitialize]
        public void Inicializar() => _repository = new CredorRepository(ContextFactory);

        [TestMethod]
        public async Task InserirAsync_PersisteCredorComObservacoesELogo()
        {
            var usuario = await CriarUsuarioAsync();
            byte[] logo = [7, 8, 9];

            var credor = await _repository.InserirAsync(new Credor
            {
                IdUsuario = usuario.Id,
                Nome = "Banco X",
                Observacoes = "Cartão de crédito",
                Logo = logo
            });

            var obtido = await _repository.ObterPorIdAsync(credor.Id);
            Assert.IsNotNull(obtido);
            Assert.AreEqual("Banco X", obtido.Nome);
            Assert.AreEqual("Cartão de crédito", obtido.Observacoes);
            CollectionAssert.AreEqual(logo, obtido.Logo);
        }

        [TestMethod]
        public async Task InserirAsync_PermiteCamposOpcionaisNulos()
        {
            var usuario = await CriarUsuarioAsync();

            var credor = await _repository.InserirAsync(new Credor { IdUsuario = usuario.Id, Nome = "Loja Y" });

            var obtido = await _repository.ObterPorIdAsync(credor.Id);
            Assert.IsNotNull(obtido);
            Assert.IsNull(obtido.Observacoes);
            Assert.IsNull(obtido.Logo);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_RetornaSomenteCredoresDoUsuario()
        {
            var usuario = await CriarUsuarioAsync("a@teste.com");
            var outro = await CriarUsuarioAsync("b@teste.com");
            await SemearAsync(
                new Credor { IdUsuario = usuario.Id, Nome = "Meu" },
                new Credor { IdUsuario = outro.Id, Nome = "De outro" });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual("Meu", lista[0].Nome);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_OrdenaPorNome()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(
                new Credor { IdUsuario = usuario.Id, Nome = "Zeta" },
                new Credor { IdUsuario = usuario.Id, Nome = "Alfa" },
                new Credor { IdUsuario = usuario.Id, Nome = "Beta" });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            CollectionAssert.AreEqual(new[] { "Alfa", "Beta", "Zeta" }, lista.Select(c => c.Nome).ToArray());
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_IgnoraInativosPorPadrao()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(
                new Credor { IdUsuario = usuario.Id, Nome = "Ativo" },
                new Credor { IdUsuario = usuario.Id, Nome = "Inativo", Ativo = false });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual("Ativo", lista[0].Nome);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_IncluiInativosQuandoSolicitado()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(
                new Credor { IdUsuario = usuario.Id, Nome = "Ativo" },
                new Credor { IdUsuario = usuario.Id, Nome = "Inativo", Ativo = false });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id, incluirInativos: true);

            Assert.HasCount(2, lista);
        }

        [TestMethod]
        public async Task AtualizarAsync_AlteraObservacoes()
        {
            var usuario = await CriarUsuarioAsync();
            var credor = await _repository.InserirAsync(new Credor { IdUsuario = usuario.Id, Nome = "Banco X" });

            credor.Observacoes = "Renegociado";
            await _repository.AtualizarAsync(credor);

            var obtido = await _repository.ObterPorIdAsync(credor.Id);
            Assert.AreEqual("Renegociado", obtido?.Observacoes);
        }

        [TestMethod]
        public async Task DesativarAsync_RemoveDaListagemDoUsuario()
        {
            var usuario = await CriarUsuarioAsync();
            var credor = await _repository.InserirAsync(new Credor { IdUsuario = usuario.Id, Nome = "Banco X" });

            await _repository.DesativarAsync(credor.Id);

            Assert.IsEmpty(await _repository.ListarPorUsuarioAsync(usuario.Id));
        }
    }
}

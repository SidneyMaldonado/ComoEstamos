using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;

namespace ComoEstamos.Test.RepositoryTests
{
    [TestClass]
    public sealed class ContaRepositoryTests : RepositoryTestBase
    {
        private ContaRepository _repository = null!;

        [TestInitialize]
        public void Inicializar() => _repository = new ContaRepository(ContextFactory);

        [TestMethod]
        public async Task InserirAsync_PersisteContaComSaldo()
        {
            var usuario = await CriarUsuarioAsync();

            var conta = await _repository.InserirAsync(new Conta { IdUsuario = usuario.Id, Nome = "Corrente", Saldo = 1500.75m });

            var obtida = await _repository.ObterPorIdAsync(conta.Id);
            Assert.IsNotNull(obtida);
            Assert.AreEqual("Corrente", obtida.Nome);
            Assert.AreEqual(1500.75m, obtida.Saldo);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_RetornaSomenteContasDoUsuarioOrdenadasPorNome()
        {
            var usuario = await CriarUsuarioAsync("a@teste.com");
            var outro = await CriarUsuarioAsync("b@teste.com");
            await SemearAsync(
                new Conta { IdUsuario = usuario.Id, Nome = "Poupança" },
                new Conta { IdUsuario = usuario.Id, Nome = "Corrente" },
                new Conta { IdUsuario = outro.Id, Nome = "De outro" });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            CollectionAssert.AreEqual(new[] { "Corrente", "Poupança" }, lista.Select(c => c.Nome).ToArray());
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_IgnoraInativosPorPadrao()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(
                new Conta { IdUsuario = usuario.Id, Nome = "Ativa" },
                new Conta { IdUsuario = usuario.Id, Nome = "Inativa", Ativo = false });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual("Ativa", lista[0].Nome);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_IncluiInativosQuandoSolicitado()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(
                new Conta { IdUsuario = usuario.Id, Nome = "Ativa" },
                new Conta { IdUsuario = usuario.Id, Nome = "Inativa", Ativo = false });

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id, incluirInativos: true);

            Assert.HasCount(2, lista);
        }

        [TestMethod]
        public async Task ObterSaldoTotalAsync_SomaSaldosDasContasAtivasDoUsuario()
        {
            var usuario = await CriarUsuarioAsync("a@teste.com");
            var outro = await CriarUsuarioAsync("b@teste.com");
            await SemearAsync(
                new Conta { IdUsuario = usuario.Id, Nome = "Corrente", Saldo = 1000.50m },
                new Conta { IdUsuario = usuario.Id, Nome = "Cartão", Saldo = -250.25m },
                new Conta { IdUsuario = usuario.Id, Nome = "Inativa", Saldo = 9999m, Ativo = false },
                new Conta { IdUsuario = outro.Id, Nome = "De outro", Saldo = 5000m });

            var total = await _repository.ObterSaldoTotalAsync(usuario.Id);

            Assert.AreEqual(750.25m, total);
        }

        [TestMethod]
        public async Task ObterSaldoTotalAsync_RetornaZeroSemContas()
        {
            var usuario = await CriarUsuarioAsync();

            var total = await _repository.ObterSaldoTotalAsync(usuario.Id);

            Assert.AreEqual(0m, total);
        }

        [TestMethod]
        public async Task AtualizarSaldoAsync_GravaNovoSaldoEDataAlteracao()
        {
            var usuario = await CriarUsuarioAsync();
            var conta = await _repository.InserirAsync(new Conta { IdUsuario = usuario.Id, Nome = "Corrente", Saldo = 100m });

            await _repository.AtualizarSaldoAsync(conta.Id, 350.40m);

            var obtida = await _repository.ObterPorIdAsync(conta.Id);
            Assert.IsNotNull(obtida);
            Assert.AreEqual(350.40m, obtida.Saldo);
            Assert.AreEqual("Corrente", obtida.Nome);
            Assert.IsNotNull(obtida.DataAlteracao);
        }

        [TestMethod]
        public async Task AtualizarSaldoAsync_LancaExcecaoQuandoContaNaoExiste()
        {
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _repository.AtualizarSaldoAsync(999, 10m));
        }

        [TestMethod]
        public async Task DesativarAsync_RemoveContaDoSaldoTotal()
        {
            var usuario = await CriarUsuarioAsync();
            var conta = await _repository.InserirAsync(new Conta { IdUsuario = usuario.Id, Nome = "Corrente", Saldo = 100m });

            await _repository.DesativarAsync(conta.Id);

            Assert.AreEqual(0m, await _repository.ObterSaldoTotalAsync(usuario.Id));
        }
    }
}

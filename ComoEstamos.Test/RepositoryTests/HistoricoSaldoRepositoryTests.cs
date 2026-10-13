using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;

namespace ComoEstamos.Test.RepositoryTests
{
    [TestClass]
    public sealed class HistoricoSaldoRepositoryTests : RepositoryTestBase
    {
        private HistoricoSaldoRepository _repository = null!;
        private Usuario _usuario = null!;
        private Conta _conta = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _repository = new HistoricoSaldoRepository(ContextFactory);
            _usuario = await CriarUsuarioAsync();
            _conta = new Conta { IdUsuario = _usuario.Id, Nome = "Nubank" };
            await SemearAsync(_conta);
        }

        private HistoricoSaldo NovoHistorico(DateTime data, int? idConta = null, decimal saldo = 100m) => new()
        {
            IdConta = idConta ?? _conta.Id,
            DataHistoricoSaldo = data,
            Saldo = saldo
        };

        [TestMethod]
        public async Task InserirAsync_PersisteSaldoDaConta()
        {
            var historico = await _repository.InserirAsync(NovoHistorico(new DateTime(2026, 10, 13, 14, 7, 54), saldo: -1250.5m));

            var obtido = await _repository.ObterPorIdAsync(historico.Id);
            Assert.IsNotNull(obtido);
            Assert.AreEqual(_conta.Id, obtido.IdConta);
            Assert.AreEqual(new DateTime(2026, 10, 13, 14, 7, 54), obtido.DataHistoricoSaldo);
            Assert.AreEqual(-1250.5m, obtido.Saldo);
            Assert.IsTrue(obtido.Ativo);
        }

        [TestMethod]
        public async Task ListarPorContaAsync_RetornaSomenteHistoricosDaConta()
        {
            var outra = new Conta { IdUsuario = _usuario.Id, Nome = "Inter" };
            await SemearAsync(outra);
            await SemearAsync(
                NovoHistorico(new DateTime(2026, 10, 1)),
                NovoHistorico(new DateTime(2026, 10, 2), outra.Id));

            var lista = await _repository.ListarPorContaAsync(_conta.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual(_conta.Id, lista[0].IdConta);
        }

        [TestMethod]
        public async Task ListarPorContaAsync_OrdenaDoMaisRecenteParaMaisAntigo()
        {
            await SemearAsync(
                NovoHistorico(new DateTime(2026, 8, 31)),
                NovoHistorico(new DateTime(2026, 10, 13)),
                NovoHistorico(new DateTime(2026, 9, 30)));

            var lista = await _repository.ListarPorContaAsync(_conta.Id);

            CollectionAssert.AreEqual(
                new[] { new DateTime(2026, 10, 13), new DateTime(2026, 9, 30), new DateTime(2026, 8, 31) },
                lista.Select(h => h.DataHistoricoSaldo).ToArray());
        }

        [TestMethod]
        public async Task ListarPorContaAsync_IgnoraInativosPorPadraoEIncluiQuandoSolicitado()
        {
            var inativo = NovoHistorico(new DateTime(2026, 10, 2));
            inativo.Ativo = false;
            await SemearAsync(NovoHistorico(new DateTime(2026, 10, 1)), inativo);

            Assert.HasCount(1, await _repository.ListarPorContaAsync(_conta.Id));
            Assert.HasCount(2, await _repository.ListarPorContaAsync(_conta.Id, incluirInativos: true));
        }
    }
}

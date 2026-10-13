using ComoEstamos.Core;
using ComoEstamos.Core.UseCases.Contas;
using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    [TestClass]
    public sealed class AtualizarSaldosUseCaseTests : RepositoryTestBase
    {
        private AtualizarSaldosUseCase _useCase = null!;
        private Usuario _usuario = null!;
        private Conta _nubank = null!;
        private Conta _inter = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _useCase = new AtualizarSaldosUseCase(new UnitOfWorkFactory(ContextFactory));
            _usuario = await CriarUsuarioAsync();
            _nubank = new Conta { IdUsuario = _usuario.Id, Nome = "Nubank", Saldo = 100m };
            _inter = new Conta { IdUsuario = _usuario.Id, Nome = "Inter", Saldo = 200m };
            await SemearAsync(_nubank, _inter);
        }

        private async Task<Dictionary<int, decimal>> SaldosAsync()
        {
            await using var db = CriarContexto();
            return await db.Contas.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Saldo);
        }

        private async Task<List<HistoricoSaldo>> HistoricosAsync()
        {
            await using var db = CriarContexto();
            return await db.HistoricosSaldo.AsNoTracking().OrderBy(h => h.IdConta).ToListAsync();
        }

        [TestMethod]
        public async Task Executar_GravaSoOsSaldosAlteradosComHistorico()
        {
            var alteradas = await _useCase.ExecutarAsync(
                [new NovoSaldo(_nubank.Id, -50.25m), new NovoSaldo(_inter.Id, 200m)], _usuario.Id);

            Assert.AreEqual(1, alteradas);
            var saldos = await SaldosAsync();
            Assert.AreEqual(-50.25m, saldos[_nubank.Id]);
            Assert.AreEqual(200m, saldos[_inter.Id]);

            var historico = (await HistoricosAsync()).Single();
            Assert.AreEqual(_nubank.Id, historico.IdConta);
            Assert.AreEqual(-50.25m, historico.Saldo);
            Assert.IsTrue(historico.Ativo);
        }

        [TestMethod]
        public async Task Executar_HistoricosDaMesmaAtualizacaoTemAMesmaData()
        {
            await _useCase.ExecutarAsync([new NovoSaldo(_nubank.Id, 1m), new NovoSaldo(_inter.Id, 2m)], _usuario.Id);

            var historicos = await HistoricosAsync();
            Assert.HasCount(2, historicos);
            Assert.AreEqual(historicos[0].DataHistoricoSaldo, historicos[1].DataHistoricoSaldo);
        }

        [TestMethod]
        public async Task Executar_SemAlteracaoNaoGravaHistorico()
        {
            var alteradas = await _useCase.ExecutarAsync([new NovoSaldo(_nubank.Id, 100m)], _usuario.Id);

            Assert.AreEqual(0, alteradas);
            Assert.IsEmpty(await HistoricosAsync());
        }

        [TestMethod]
        [DataRow(10.555)]
        [DataRow(100000000.0)]
        [DataRow(-100000000.0)]
        public async Task Executar_SaldoInvalidoRecusaTudoSemGravar(double invalido)
        {
            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(() => _useCase.ExecutarAsync(
                [new NovoSaldo(_nubank.Id, 1m), new NovoSaldo(_inter.Id, (decimal)invalido)], _usuario.Id));

            Assert.AreEqual(AtualizarSaldosUseCase.CampoSaldo(_inter.Id), ex.Erros.Single().Campo);
            Assert.AreEqual(100m, (await SaldosAsync())[_nubank.Id]);
            Assert.IsEmpty(await HistoricosAsync());
        }

        [TestMethod]
        public async Task Executar_AceitaOsLimitesDoSaldo()
        {
            await _useCase.ExecutarAsync(
                [new NovoSaldo(_nubank.Id, Limites.Decimal10x2), new NovoSaldo(_inter.Id, -Limites.Decimal10x2)], _usuario.Id);

            var saldos = await SaldosAsync();
            Assert.AreEqual(Limites.Decimal10x2, saldos[_nubank.Id]);
            Assert.AreEqual(-Limites.Decimal10x2, saldos[_inter.Id]);
        }

        [TestMethod]
        public async Task Executar_ContaDeOutroUsuarioOuInexistenteNaoGravaNada()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var alheia = new Conta { IdUsuario = outro.Id, Nome = "Alheia" };
            await SemearAsync(alheia);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync(
                [new NovoSaldo(_nubank.Id, 1m), new NovoSaldo(alheia.Id, 1m)], _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync(
                [new NovoSaldo(_nubank.Id, 1m), new NovoSaldo(999, 1m)], _usuario.Id));

            Assert.AreEqual(100m, (await SaldosAsync())[_nubank.Id]);
            Assert.IsEmpty(await HistoricosAsync());
        }

        [TestMethod]
        public async Task Executar_ContaInativaRecusada()
        {
            var inativa = new Conta { IdUsuario = _usuario.Id, Nome = "Antiga", Ativo = false };
            await SemearAsync(inativa);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(
                [new NovoSaldo(inativa.Id, 1m)], _usuario.Id));
        }

        [TestMethod]
        public async Task Executar_ContaRepetidaRecusada()
        {
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => _useCase.ExecutarAsync(
                [new NovoSaldo(_nubank.Id, 1m), new NovoSaldo(_nubank.Id, 2m)], _usuario.Id));
        }
    }
}

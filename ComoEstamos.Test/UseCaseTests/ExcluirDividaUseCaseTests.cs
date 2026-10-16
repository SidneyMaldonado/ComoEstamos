using ComoEstamos.Core;
using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    [TestClass]
    public sealed class ExcluirDividaUseCaseTests : RepositoryTestBase
    {
        private ExcluirDividaUseCase _useCase = null!;
        private Usuario _usuario = null!;
        private Conta _conta = null!;
        private Categoria _categoria = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _useCase = new ExcluirDividaUseCase(new UnitOfWorkFactory(ContextFactory));
            _usuario = await CriarUsuarioAsync();
            _conta = new Conta { IdUsuario = _usuario.Id, Nome = "Corrente", Saldo = 500m };
            _categoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Compras" };
            await SemearAsync(_conta, _categoria);
        }

        /// <summary>Dívida com 3 parcelas, a primeira paga.</summary>
        private async Task<Divida> CriarDividaAsync(int? idConta = null, bool ativa = true)
        {
            var divida = new Divida
            {
                IdConta = idConta ?? _conta.Id,
                IdCategoria = _categoria.Id,
                Nome = "Notebook",
                NumeroParcelas = 3,
                Valor = 300m,
                DataPrimeiroVencimento = new DateTime(2026, 10, 10),
                Ativo = ativa
            };
            divida.Parcelas = Enumerable.Range(0, 3).Select(i => new Parcela
            {
                Divida = divida,
                IdConta = _conta.Id,
                Descricao = $"Notebook {i + 1}/3",
                Valor = 100m,
                DataVencimento = new DateTime(2026, 10 + i, 10),
                DataPagamento = i == 0 ? new DateTime(2026, 10, 10) : null
            }).ToList();
            await SemearAsync(divida);
            return divida;
        }

        [TestMethod]
        public async Task ExecutarAsync_DesativaADividaETodasAsParcelasSemMexerNoSaldo()
        {
            var divida = await CriarDividaAsync();

            await _useCase.ExecutarAsync(divida.Id, _usuario.Id);

            await using var db = CriarContexto();
            Assert.IsFalse((await db.Dividas.SingleAsync()).Ativo);
            Assert.AreEqual(3, await db.Parcelas.CountAsync(), "Nada é apagado do banco.");
            Assert.IsFalse(await db.Parcelas.AnyAsync(p => p.Ativo), "Pagas e em aberto ficam desativadas.");
            Assert.IsNotNull((await db.Parcelas.SingleAsync(p => p.Descricao == "Notebook 1/3")).DataPagamento);
            Assert.AreEqual(500m, (await db.Contas.SingleAsync()).Saldo);
        }

        [TestMethod]
        public async Task ExecutarAsync_LiberaAExclusaoDaConta()
        {
            var divida = await CriarDividaAsync();

            await _useCase.ExecutarAsync(divida.Id, _usuario.Id);

            Assert.IsFalse(await new DividaRepository(ContextFactory).ExisteAtivaPorContaAsync(_conta.Id));
            Assert.IsFalse(await new ParcelaRepository(ContextFactory).ExisteAtivaPorContaAsync(_conta.Id));
        }

        [TestMethod]
        public async Task ExecutarAsync_InexistenteOuDeOutroUsuarioLancaKeyNotFoundSemDesativar()
        {
            var contaDeOutro = await CriarContaDeOutroUsuarioAsync();
            var deOutro = await CriarDividaAsync(contaDeOutro.Id);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync(999, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync(deOutro.Id, _usuario.Id));

            await using var db = CriarContexto();
            Assert.IsTrue((await db.Dividas.SingleAsync()).Ativo);
            Assert.IsTrue(await db.Parcelas.AllAsync(p => p.Ativo));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaDividaJaExcluida()
        {
            var divida = await CriarDividaAsync(ativa: false);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(divida.Id, _usuario.Id));
        }
    }
}

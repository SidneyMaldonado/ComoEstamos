using ComoEstamos.Core;
using ComoEstamos.Core.UseCases;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    [TestClass]
    public sealed class PagarParcelaUseCaseTests : RepositoryTestBase
    {
        private PagarParcelaUseCase _useCase = null!;
        private Usuario _usuario = null!;
        private Conta _conta = null!;
        private Categoria _categoria = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _useCase = new PagarParcelaUseCase(new UnitOfWorkFactory(ContextFactory));
            _usuario = await CriarUsuarioAsync();
            _conta = new Conta { IdUsuario = _usuario.Id, Nome = "Corrente", Saldo = 1000m };
            _categoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Moradia" };
            await SemearAsync(_conta, _categoria);
        }

        private async Task<Parcela> CriarParcelaAsync(bool ehDivida = true, decimal valor = 250m, bool dividaAtiva = true)
        {
            var divida = new Divida { IdUsuario = _usuario.Id, Nome = "Aluguel", Valor = valor, EhDivida = ehDivida, Ativo = dividaAtiva };
            await SemearAsync(divida);
            var parcela = new Parcela
            {
                IdDivida = divida.Id,
                IdConta = _conta.Id,
                IdCategoria = _categoria.Id,
                Descricao = "Aluguel 1/1",
                Valor = valor,
                DataVencimento = new DateTime(2026, 10, 10)
            };
            await SemearAsync(parcela);
            return parcela;
        }

        private async Task<(Parcela Parcela, Conta Conta)> LerDoBancoAsync(int idParcela)
        {
            await using var db = CriarContexto();
            return (await db.Parcelas.SingleAsync(p => p.Id == idParcela), await db.Contas.SingleAsync(c => c.Id == _conta.Id));
        }

        [TestMethod]
        public async Task ExecutarAsync_DividaRegistraPagamentoEDebitaConta()
        {
            var parcela = await CriarParcelaAsync(ehDivida: true, valor: 250m);

            await _useCase.ExecutarAsync(parcela.Id, new DateTime(2026, 10, 8));

            var (paga, conta) = await LerDoBancoAsync(parcela.Id);
            Assert.AreEqual(new DateTime(2026, 10, 8), paga.DataPagamento);
            Assert.AreEqual(750m, conta.Saldo);
        }

        [TestMethod]
        public async Task ExecutarAsync_CreditoRegistraRecebimentoECreditaConta()
        {
            var parcela = await CriarParcelaAsync(ehDivida: false, valor: 250m);

            await _useCase.ExecutarAsync(parcela.Id, new DateTime(2026, 10, 8));

            var (paga, conta) = await LerDoBancoAsync(parcela.Id);
            Assert.AreEqual(new DateTime(2026, 10, 8), paga.DataPagamento);
            Assert.AreEqual(1250m, conta.Saldo);
        }

        [TestMethod]
        public async Task ExecutarAsync_PermiteSaldoNegativo()
        {
            var parcela = await CriarParcelaAsync(valor: 1500m);

            await _useCase.ExecutarAsync(parcela.Id, DateTime.Today);

            var (_, conta) = await LerDoBancoAsync(parcela.Id);
            Assert.AreEqual(-500m, conta.Saldo);
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaParcelaJaPagaSemAlterarSaldo()
        {
            var parcela = await CriarParcelaAsync();
            await _useCase.ExecutarAsync(parcela.Id, new DateTime(2026, 10, 8));

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => _useCase.ExecutarAsync(parcela.Id, new DateTime(2026, 10, 9)));

            var (paga, conta) = await LerDoBancoAsync(parcela.Id);
            Assert.AreEqual(new DateTime(2026, 10, 8), paga.DataPagamento);
            Assert.AreEqual(750m, conta.Saldo);
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaParcelaDesativada()
        {
            var parcela = await CriarParcelaAsync();
            await using (var db = CriarContexto())
            {
                (await db.Parcelas.SingleAsync()).Ativo = false;
                await db.SaveChangesAsync();
            }

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(parcela.Id, DateTime.Today));

            var (naoPaga, conta) = await LerDoBancoAsync(parcela.Id);
            Assert.IsNull(naoPaga.DataPagamento);
            Assert.AreEqual(1000m, conta.Saldo);
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaParcelaDeDividaDesativada()
        {
            var parcela = await CriarParcelaAsync(dividaAtiva: false);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(parcela.Id, DateTime.Today));

            var (naoPaga, conta) = await LerDoBancoAsync(parcela.Id);
            Assert.IsNull(naoPaga.DataPagamento);
            Assert.AreEqual(1000m, conta.Saldo);
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaContaDesativada()
        {
            var parcela = await CriarParcelaAsync();
            await using (var db = CriarContexto())
            {
                (await db.Contas.SingleAsync()).Ativo = false;
                await db.SaveChangesAsync();
            }

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(parcela.Id, DateTime.Today));

            var (naoPaga, _) = await LerDoBancoAsync(parcela.Id);
            Assert.IsNull(naoPaga.DataPagamento);
        }

        [TestMethod]
        public async Task ExecutarAsync_LancaExcecaoQuandoParcelaNaoExiste()
        {
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync(999, DateTime.Today));
        }
    }
}

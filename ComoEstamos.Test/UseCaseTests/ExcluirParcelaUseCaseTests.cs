using ComoEstamos.Core;
using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Core.UseCases.Parcelas;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    [TestClass]
    public sealed class ExcluirParcelaUseCaseTests : RepositoryTestBase
    {
        private ExcluirParcelaUseCase _useCase = null!;
        private CriarDividaUseCase _criar = null!;
        private Usuario _usuario = null!;
        private Conta _conta = null!;
        private Categoria _categoria = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            var uow = new UnitOfWorkFactory(ContextFactory);
            _useCase = new ExcluirParcelaUseCase(uow);
            _criar = new CriarDividaUseCase(uow);
            _usuario = await CriarUsuarioAsync();
            _conta = new Conta { IdUsuario = _usuario.Id, Nome = "Corrente", Saldo = 1000m };
            _categoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Compras" };
            await SemearAsync(_conta, _categoria);
        }

        /// <summary>Cria pela inclusão (R$ 100 por parcela, dia 10, a partir de 10/10/2026) e paga as <paramref name="pagas"/> primeiras.</summary>
        private async Task<Divida> CriarAsync(int parcelas = 3, int pagas = 0)
        {
            var divida = await _criar.ExecutarAsync(new Divida
            {
                IdConta = _conta.Id,
                IdCategoria = _categoria.Id,
                Nome = "Notebook",
                Valor = 100m * parcelas,
                NumeroParcelas = parcelas,
                DiaVencimento = 10,
                DataPrimeiroVencimento = new DateTime(2026, 10, 10)
            }, _usuario.Id);

            await using var db = CriarContexto();
            foreach (var parcela in db.Parcelas.OrderBy(p => p.DataVencimento).Take(pagas))
                parcela.DataPagamento = parcela.DataVencimento;
            await db.SaveChangesAsync();
            return divida;
        }

        /// <summary>Parcelas ativas gravadas, por vencimento.</summary>
        private async Task<List<Parcela>> ParcelasAsync()
        {
            await using var db = CriarContexto();
            return await db.Parcelas.Where(p => p.Ativo).OrderBy(p => p.DataVencimento).ToListAsync();
        }

        private async Task<Divida> DividaGravadaAsync()
        {
            await using var db = CriarContexto();
            return await db.Dividas.SingleAsync();
        }

        [TestMethod]
        public async Task ExecutarAsync_DesativaERecalculaADividaSemMexerNoSaldo()
        {
            await CriarAsync();
            var segunda = (await ParcelasAsync())[1];

            await _useCase.ExecutarAsync(segunda.Id, _usuario.Id);

            var restantes = await ParcelasAsync();
            CollectionAssert.AreEqual(new[] { "Notebook 1/2", "Notebook 2/2" }, restantes.Select(p => p.Descricao).ToList());
            Assert.IsFalse(restantes.Any(p => p.Id == segunda.Id));

            var divida = await DividaGravadaAsync();
            Assert.AreEqual(200m, divida.Valor);
            Assert.AreEqual(2, divida.NumeroParcelas);
            Assert.AreEqual(new DateTime(2026, 10, 10), divida.DataPrimeiroVencimento);

            await using var db = CriarContexto();
            Assert.AreEqual(3, await db.Parcelas.CountAsync(), "Nada é apagado do banco.");
            Assert.AreEqual(1000m, (await db.Contas.SingleAsync()).Saldo);
        }

        [TestMethod]
        public async Task ExecutarAsync_ExcluirAPrimeiraAtualizaOPrimeiroVencimento()
        {
            await CriarAsync();

            await _useCase.ExecutarAsync((await ParcelasAsync())[0].Id, _usuario.Id);

            Assert.AreEqual(new DateTime(2026, 11, 10), (await DividaGravadaAsync()).DataPrimeiroVencimento);
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaParcelaPagaSemExcluir()
        {
            await CriarAsync(pagas: 1);
            var paga = (await ParcelasAsync())[0];

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(paga.Id, _usuario.Id));

            Assert.HasCount(3, await ParcelasAsync());
            Assert.AreEqual(300m, (await DividaGravadaAsync()).Valor);
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaAUnicaParcelaDaDivida()
        {
            await CriarAsync(parcelas: 2);
            var parcelas = await ParcelasAsync();
            await _useCase.ExecutarAsync(parcelas[0].Id, _usuario.Id);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(parcelas[1].Id, _usuario.Id));

            Assert.HasCount(1, await ParcelasAsync());
            Assert.AreEqual(1, (await DividaGravadaAsync()).NumeroParcelas);
        }

        [TestMethod]
        public async Task ExecutarAsync_InexistenteOuDeOutroUsuarioLancaKeyNotFoundSemExcluir()
        {
            await CriarAsync();
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var primeira = (await ParcelasAsync())[0];

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync(999, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync(primeira.Id, outro.Id));

            Assert.HasCount(3, await ParcelasAsync());
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaParcelaJaExcluida()
        {
            await CriarAsync();
            var primeira = (await ParcelasAsync())[0];
            await _useCase.ExecutarAsync(primeira.Id, _usuario.Id);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(primeira.Id, _usuario.Id));
        }
    }
}

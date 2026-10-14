using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Core.UseCases.Parcelas;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    [TestClass]
    public sealed class BuscarParcelaPorIdUseCaseTests : RepositoryTestBase
    {
        private BuscarParcelaPorIdUseCase _useCase = null!;
        private Usuario _usuario = null!;
        private Conta _conta = null!;
        private Categoria _categoria = null!;
        private Divida _divida = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            var uow = new UnitOfWorkFactory(ContextFactory);
            _useCase = new BuscarParcelaPorIdUseCase(uow);
            _usuario = await CriarUsuarioAsync();
            _conta = new Conta { IdUsuario = _usuario.Id, Nome = "Corrente" };
            _categoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Compras" };
            await SemearAsync(_conta, _categoria);
            _divida = await new CriarDividaUseCase(uow).ExecutarAsync(new Divida
            {
                IdConta = _conta.Id,
                IdCategoria = _categoria.Id,
                Nome = "Notebook",
                Valor = 300m,
                NumeroParcelas = 3,
                DiaVencimento = 10,
                DataPrimeiroVencimento = new DateTime(2026, 10, 10)
            }, _usuario.Id);
        }

        private async Task<List<Parcela>> ParcelasAsync()
        {
            await using var db = CriarContexto();
            return await db.Parcelas.OrderBy(p => p.DataVencimento).ToListAsync();
        }

        [TestMethod]
        public async Task ExecutarAsync_DevolveComContaCategoriaEADividaComAsParcelas()
        {
            var segunda = (await ParcelasAsync())[1];

            var parcela = await _useCase.ExecutarAsync(segunda.Id, _usuario.Id);

            Assert.IsNotNull(parcela);
            Assert.AreEqual("Notebook 2/3", parcela.Descricao);
            Assert.AreEqual("Corrente", parcela.Conta?.Nome);
            Assert.AreEqual("Compras", parcela.Categoria?.Nome);
            Assert.AreEqual("Notebook", parcela.Divida?.Nome);
            Assert.AreEqual(1, parcela.Divida!.Parcelas.FindIndex(p => p.Id == segunda.Id), "Parcelas da dívida por vencimento.");
            Assert.HasCount(3, parcela.Divida.Parcelas);
        }

        [TestMethod]
        public async Task ExecutarAsync_NullParaInexistenteDeOutroUsuarioEExcluida()
        {
            var parcelas = await ParcelasAsync();
            var outro = await CriarUsuarioAsync("outro@teste.com");
            await new ExcluirParcelaUseCase(new UnitOfWorkFactory(ContextFactory)).ExecutarAsync(parcelas[2].Id, _usuario.Id);

            Assert.IsNull(await _useCase.ExecutarAsync(999, _usuario.Id));
            Assert.IsNull(await _useCase.ExecutarAsync(parcelas[0].Id, outro.Id));
            Assert.IsNull(await _useCase.ExecutarAsync(parcelas[2].Id, _usuario.Id));
        }

        [TestMethod]
        public async Task ExecutarAsync_NullQuandoADividaFoiExcluida()
        {
            var parcelas = await ParcelasAsync();
            await new ExcluirDividaUseCase(new UnitOfWorkFactory(ContextFactory)).ExecutarAsync(_divida.Id, _usuario.Id);

            Assert.IsNull(await _useCase.ExecutarAsync(parcelas[0].Id, _usuario.Id));
        }
    }
}

using ComoEstamos.Core;
using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Core.UseCases.Parcelas;
using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    [TestClass]
    public sealed class AlterarParcelaUseCaseTests : RepositoryTestBase
    {
        private AlterarParcelaUseCase _useCase = null!;
        private CriarDividaUseCase _criar = null!;
        private Usuario _usuario = null!;
        private Conta _conta = null!;
        private Conta _outraConta = null!;
        private Categoria _categoria = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            var uow = new UnitOfWorkFactory(ContextFactory);
            _useCase = new AlterarParcelaUseCase(uow);
            _criar = new CriarDividaUseCase(uow);
            _usuario = await CriarUsuarioAsync();
            _conta = new Conta { IdUsuario = _usuario.Id, Nome = "Corrente", Saldo = 1000m };
            _outraConta = new Conta { IdUsuario = _usuario.Id, Nome = "Poupança" };
            _categoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Compras" };
            await SemearAsync(_conta, _outraConta, _categoria);
        }

        /// <summary>Cria pela inclusão (R$ 300 em 3x, dia 10, a partir de 10/10/2026) e paga as <paramref name="pagas"/> primeiras.</summary>
        private async Task<Divida> CriarAsync(int pagas = 0)
        {
            var divida = await _criar.ExecutarAsync(new Divida
            {
                IdConta = _conta.Id,
                IdCategoria = _categoria.Id,
                Nome = "Notebook",
                Valor = 300m,
                NumeroParcelas = 3,
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

        /// <summary>Cópia dos campos editáveis da parcela, para alterar.</summary>
        private static Parcela Edicao(Parcela parcela) => new()
        {
            Id = parcela.Id,
            Valor = parcela.Valor,
            DataVencimento = parcela.DataVencimento,
            IdConta = parcela.IdConta
        };

        [TestMethod]
        public async Task ExecutarAsync_GravaOsCamposERecalculaOTotalDaDivida()
        {
            await CriarAsync();
            var segunda = (await ParcelasAsync())[1];
            var edicao = Edicao(segunda);
            edicao.Valor = 150m;
            edicao.DataVencimento = new DateTime(2026, 11, 15, 9, 0, 0);
            edicao.IdConta = _outraConta.Id;

            await _useCase.ExecutarAsync(edicao, _usuario.Id);

            var parcelas = await ParcelasAsync();
            var alterada = parcelas.Single(p => p.Id == segunda.Id);
            Assert.AreEqual(150m, alterada.Valor);
            Assert.AreEqual(new DateTime(2026, 11, 15), alterada.DataVencimento, "Só a data, sem hora.");
            Assert.AreEqual(_outraConta.Id, alterada.IdConta);
            Assert.IsTrue(parcelas.Where(p => p.Id != segunda.Id).All(p => p.Valor == 100m && p.IdConta == _conta.Id));

            var divida = await DividaGravadaAsync();
            Assert.AreEqual(350m, divida.Valor, "O total passa a ser a soma das parcelas.");
            Assert.AreEqual(3, divida.NumeroParcelas);
            await using var db = CriarContexto();
            Assert.AreEqual(1000m, (await db.Contas.SingleAsync(c => c.Id == _conta.Id)).Saldo, "Alterar não mexe no saldo.");
        }

        [TestMethod]
        public async Task ExecutarAsync_MudarOVencimentoRenumeraAsDescricoesEAtualizaOPrimeiroVencimento()
        {
            await CriarAsync();
            var terceira = (await ParcelasAsync())[2];
            var edicao = Edicao(terceira);
            edicao.DataVencimento = new DateTime(2026, 10, 5);

            await _useCase.ExecutarAsync(edicao, _usuario.Id);

            var descricoes = (await ParcelasAsync()).Select(p => p.Descricao).ToList();
            CollectionAssert.AreEqual(new[] { "Notebook 1/3", "Notebook 2/3", "Notebook 3/3" }, descricoes);
            Assert.AreEqual("Notebook 1/3", (await ParcelasAsync()).Single(p => p.Id == terceira.Id).Descricao);
            Assert.AreEqual(new DateTime(2026, 10, 5), (await DividaGravadaAsync()).DataPrimeiroVencimento);
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaParcelaPagaSemAlterar()
        {
            await CriarAsync(pagas: 1);
            var paga = (await ParcelasAsync())[0];
            var edicao = Edicao(paga);
            edicao.Valor = 50m;

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(edicao, _usuario.Id));

            Assert.AreEqual(100m, (await ParcelasAsync())[0].Valor);
            Assert.AreEqual(300m, (await DividaGravadaAsync()).Valor);
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaCadaRegraDeDominioSemGravar()
        {
            await CriarAsync();
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var contaDeOutro = new Conta { IdUsuario = outro.Id, Nome = "Dele" };
            await SemearAsync(contaDeOutro);
            var edicao = Edicao((await ParcelasAsync())[1]);
            edicao.Valor = 10.555m;
            edicao.IdConta = contaDeOutro.Id;

            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(() => _useCase.ExecutarAsync(edicao, _usuario.Id));

            CollectionAssert.AreEquivalent(
                new[] { nameof(Parcela.Valor), nameof(Parcela.IdConta) },
                ex.Erros.Select(e => e.Campo).ToList());
            Assert.IsTrue((await ParcelasAsync()).All(p => p.Valor == 100m && p.IdConta == _conta.Id));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaValorZeroEContaInativa()
        {
            await CriarAsync();
            await using (var db = CriarContexto())
            {
                (await db.Contas.SingleAsync(c => c.Id == _outraConta.Id)).Ativo = false;
                await db.SaveChangesAsync();
            }
            var edicao = Edicao((await ParcelasAsync())[1]);
            edicao.Valor = 0m;
            edicao.IdConta = _outraConta.Id;

            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(() => _useCase.ExecutarAsync(edicao, _usuario.Id));

            CollectionAssert.AreEquivalent(
                new[] { nameof(Parcela.Valor), nameof(Parcela.IdConta) },
                ex.Erros.Select(e => e.Campo).ToList());
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaTotalDaDividaAcimaDoLimite()
        {
            await CriarAsync();
            var edicao = Edicao((await ParcelasAsync())[1]);
            edicao.Valor = Limites.Decimal10x2;

            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(() => _useCase.ExecutarAsync(edicao, _usuario.Id));

            Assert.AreEqual(nameof(Parcela.Valor), ex.Erros.Single().Campo);
            Assert.AreEqual(300m, (await DividaGravadaAsync()).Valor);
        }

        [TestMethod]
        public async Task ExecutarAsync_InexistenteOuDeOutroUsuarioLancaKeyNotFound()
        {
            await CriarAsync();
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var edicao = Edicao((await ParcelasAsync())[0]);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync(edicao, outro.Id));
            edicao.Id = 999;
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync(edicao, _usuario.Id));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaParcelaOuDividaExcluida()
        {
            var divida = await CriarAsync();
            var parcelas = await ParcelasAsync();
            await using (var db = CriarContexto())
            {
                (await db.Parcelas.SingleAsync(p => p.Id == parcelas[0].Id)).Ativo = false;
                await db.SaveChangesAsync();
            }

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(Edicao(parcelas[0]), _usuario.Id));

            await new ExcluirDividaUseCase(new UnitOfWorkFactory(ContextFactory)).ExecutarAsync(divida.Id, _usuario.Id);
            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(Edicao(parcelas[1]), _usuario.Id));
        }
    }
}

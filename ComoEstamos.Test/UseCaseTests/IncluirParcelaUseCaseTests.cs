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
    public sealed class IncluirParcelaUseCaseTests : RepositoryTestBase
    {
        /// <summary>Máximo de parcelas de uma dívida (DividaValidador.MaximoParcelas).</summary>
        private const int MaximoParcelas = 360;

        private IncluirParcelaUseCase _useCase = null!;
        private CriarDividaUseCase _criar = null!;
        private Usuario _usuario = null!;
        private Conta _conta = null!;
        private Conta _outraConta = null!;
        private Categoria _categoria = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            var uow = new UnitOfWorkFactory(ContextFactory);
            _useCase = new IncluirParcelaUseCase(uow);
            _criar = new CriarDividaUseCase(uow);
            _usuario = await CriarUsuarioAsync();
            _conta = new Conta { IdUsuario = _usuario.Id, Nome = "Corrente", Saldo = 1000m };
            _outraConta = new Conta { IdUsuario = _usuario.Id, Nome = "Poupança" };
            _categoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Compras" };
            await SemearAsync(_conta, _outraConta, _categoria);
        }

        /// <summary>Cria pela inclusão (R$ 100 por parcela, dia 10, a partir de 10/10/2026).</summary>
        private async Task<Divida> CriarAsync(int parcelas = 3) =>
            await _criar.ExecutarAsync(new Divida
            {
                IdConta = _conta.Id,
                IdCategoria = _categoria.Id,
                Nome = "Notebook",
                Valor = 100m * parcelas,
                NumeroParcelas = parcelas,
                DiaVencimento = 10,
                DataPrimeiroVencimento = new DateTime(2026, 10, 10)
            }, _usuario.Id);

        /// <summary>Parcela nova da dívida, em 10/01/2027, com a conta e a categoria padrão.</summary>
        private Parcela Nova(int idDivida, decimal valor = 100m, DateTime? vencimento = null) => new()
        {
            IdDivida = idDivida,
            Valor = valor,
            DataVencimento = vencimento ?? new DateTime(2027, 1, 10),
            IdConta = _conta.Id,
            IdCategoria = _categoria.Id
        };

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
        public async Task ExecutarAsync_GravaEmAbertoERecalculaADivida()
        {
            var divida = await CriarAsync();
            var nova = Nova(divida.Id, 150m, new DateTime(2027, 1, 10, 8, 0, 0));
            nova.IdConta = _outraConta.Id;
            nova.DataPagamento = DateTime.Today;
            nova.Ativo = false;

            var gravada = await _useCase.ExecutarAsync(nova, _usuario.Id);

            var parcelas = await ParcelasAsync();
            Assert.HasCount(4, parcelas);
            var incluida = parcelas.Single(p => p.Id == gravada.Id);
            Assert.AreEqual(150m, incluida.Valor);
            Assert.AreEqual(new DateTime(2027, 1, 10), incluida.DataVencimento, "Só a data, sem hora.");
            Assert.AreEqual(_outraConta.Id, incluida.IdConta);
            Assert.IsNull(incluida.DataPagamento, "Nasce em aberto.");
            Assert.IsTrue(incluida.Ativo);
            CollectionAssert.AreEqual(
                new[] { "Notebook 1/4", "Notebook 2/4", "Notebook 3/4", "Notebook 4/4" },
                parcelas.Select(p => p.Descricao).ToList());

            var gravadaDivida = await DividaGravadaAsync();
            Assert.AreEqual(450m, gravadaDivida.Valor);
            Assert.AreEqual(4, gravadaDivida.NumeroParcelas);
            await using var db = CriarContexto();
            Assert.AreEqual(1000m, (await db.Contas.SingleAsync(c => c.Id == _conta.Id)).Saldo, "Incluir não mexe no saldo.");
        }

        [TestMethod]
        public async Task ExecutarAsync_AntesDaPrimeiraRenumeraEAtualizaOPrimeiroVencimento()
        {
            var divida = await CriarAsync();

            var incluida = await _useCase.ExecutarAsync(Nova(divida.Id, vencimento: new DateTime(2026, 9, 10)), _usuario.Id);

            var parcelas = await ParcelasAsync();
            Assert.AreEqual(incluida.Id, parcelas[0].Id);
            Assert.AreEqual("Notebook 1/4", parcelas[0].Descricao);
            Assert.AreEqual(new DateTime(2026, 9, 10), (await DividaGravadaAsync()).DataPrimeiroVencimento);
        }

        [TestMethod]
        public async Task ExecutarAsync_MesmoVencimentoDeOutraFicaDepoisDela()
        {
            var divida = await CriarAsync();
            var segunda = (await ParcelasAsync())[1];

            var incluida = await _useCase.ExecutarAsync(Nova(divida.Id, vencimento: segunda.DataVencimento), _usuario.Id);

            var parcelas = await ParcelasAsync();
            Assert.AreEqual("Notebook 2/4", parcelas.Single(p => p.Id == segunda.Id).Descricao);
            Assert.AreEqual("Notebook 3/4", parcelas.Single(p => p.Id == incluida.Id).Descricao);
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaCadaRegraDeDominioSemGravar()
        {
            var divida = await CriarAsync();
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var contaDeOutro = new Conta { IdUsuario = outro.Id, Nome = "Dele" };
            await SemearAsync(contaDeOutro);
            var nova = Nova(divida.Id, 0m);
            nova.IdConta = contaDeOutro.Id;
            nova.IdCategoria = 0;

            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(() => _useCase.ExecutarAsync(nova, _usuario.Id));

            CollectionAssert.AreEquivalent(
                new[] { nameof(Parcela.Valor), nameof(Parcela.IdConta), nameof(Parcela.IdCategoria) },
                ex.Erros.Select(e => e.Campo).ToList());
            Assert.HasCount(3, await ParcelasAsync());
            Assert.AreEqual(300m, (await DividaGravadaAsync()).Valor);
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaTotalDaDividaAcimaDoLimite()
        {
            var divida = await CriarAsync();

            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(
                () => _useCase.ExecutarAsync(Nova(divida.Id, Limites.Decimal10x2), _usuario.Id));

            Assert.AreEqual(nameof(Parcela.Valor), ex.Erros.Single().Campo);
            Assert.HasCount(3, await ParcelasAsync());
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaPassarDoMaximoDeParcelas()
        {
            var divida = await CriarAsync(MaximoParcelas);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(Nova(divida.Id), _usuario.Id));

            Assert.HasCount(MaximoParcelas, await ParcelasAsync());
        }

        [TestMethod]
        public async Task ExecutarAsync_DividaInexistenteOuDeOutroUsuarioLancaKeyNotFoundSemGravar()
        {
            var divida = await CriarAsync();
            var outro = await CriarUsuarioAsync("outro@teste.com");

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync(Nova(999), _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync(Nova(divida.Id), outro.Id));

            Assert.HasCount(3, await ParcelasAsync());
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaDividaExcluida()
        {
            var divida = await CriarAsync();
            await new ExcluirDividaUseCase(new UnitOfWorkFactory(ContextFactory)).ExecutarAsync(divida.Id, _usuario.Id);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(Nova(divida.Id), _usuario.Id));

            await using var db = CriarContexto();
            Assert.AreEqual(3, await db.Parcelas.CountAsync());
        }
    }
}

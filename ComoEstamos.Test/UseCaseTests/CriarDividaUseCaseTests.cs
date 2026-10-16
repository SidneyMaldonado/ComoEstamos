using ComoEstamos.Core;
using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    [TestClass]
    public sealed class CriarDividaUseCaseTests : RepositoryTestBase
    {
        private CriarDividaUseCase _useCase = null!;
        private Usuario _usuario = null!;
        private Conta _conta = null!;
        private Categoria _categoria = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _useCase = new CriarDividaUseCase(new UnitOfWorkFactory(ContextFactory));
            _usuario = await CriarUsuarioAsync();
            _conta = new Conta { IdUsuario = _usuario.Id, Nome = "Corrente" };
            _categoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Moradia" };
            await SemearAsync(_conta, _categoria);
        }

        private Divida NovaDivida(decimal valor = 1200m, int parcelas = 12) => new()
        {
            IdUsuario = _usuario.Id,
            IdConta = _conta.Id,
            IdCategoria = _categoria.Id,
            Nome = "Notebook",
            Valor = valor,
            NumeroParcelas = parcelas,
            DiaVencimento = 10,
            DataPrimeiroVencimento = new DateTime(2026, 10, 10)
        };

        private Task<Divida> ExecutarAsync(Divida divida) => _useCase.ExecutarAsync(divida, _usuario.Id);

        /// <summary>Confere que a dívida é recusada com erro no campo informado e que nada foi gravado.</summary>
        private async Task AssertRecusaCampoAsync(Divida divida, string campo)
        {
            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(() => ExecutarAsync(divida));
            Assert.IsTrue(ex.Erros.Any(e => e.Campo == campo), $"Esperava erro no campo {campo}.");
            await AssertNadaGravadoAsync();
        }

        private async Task<List<Parcela>> ParcelasGravadasAsync()
        {
            await using var db = CriarContexto();
            return await db.Parcelas.OrderBy(p => p.DataVencimento).ToListAsync();
        }

        private async Task AssertNadaGravadoAsync()
        {
            await using var db = CriarContexto();
            Assert.AreEqual(0, await db.Dividas.CountAsync());
            Assert.AreEqual(0, await db.Parcelas.CountAsync());
        }

        [TestMethod]
        public async Task ExecutarAsync_GravaDividaEGeraUmaParcelaPorMes()
        {
            var divida = await ExecutarAsync(NovaDivida(1200m, 12));

            Assert.IsGreaterThan(0, divida.Id);
            var parcelas = await ParcelasGravadasAsync();
            Assert.HasCount(12, parcelas);
            Assert.IsTrue(parcelas.All(p => p.IdDivida == divida.Id));
            Assert.IsTrue(parcelas.All(p => p.IdConta == _conta.Id));
            Assert.IsTrue(parcelas.All(p => p.Valor == 100m && p.DataPagamento is null && p.Ativo));
            Assert.AreEqual(new DateTime(2026, 10, 10), parcelas[0].DataVencimento);
            Assert.AreEqual(new DateTime(2027, 9, 10), parcelas[11].DataVencimento);
        }

        [TestMethod]
        public async Task ExecutarAsync_RetornaDividaComAsParcelas()
        {
            var divida = await ExecutarAsync(NovaDivida(300m, 3));

            Assert.HasCount(3, divida.Parcelas);
        }

        [TestMethod]
        public async Task ExecutarAsync_ColocaCentavosDoArredondamentoNaUltimaParcela()
        {
            await ExecutarAsync(NovaDivida(100m, 3));

            var parcelas = await ParcelasGravadasAsync();
            CollectionAssert.AreEqual(new[] { 33.33m, 33.33m, 33.34m }, parcelas.Select(p => p.Valor).ToArray());
            Assert.AreEqual(100m, parcelas.Sum(p => p.Valor));
        }

        [TestMethod]
        public async Task ExecutarAsync_ParcelaUnicaRecebeValorTotal()
        {
            await ExecutarAsync(NovaDivida(99.99m, 1));

            var parcelas = await ParcelasGravadasAsync();
            Assert.HasCount(1, parcelas);
            Assert.AreEqual(99.99m, parcelas[0].Valor);
        }

        [TestMethod]
        public async Task ExecutarAsync_UsaUltimoDiaDoMesQuandoDiaVencimentoNaoExiste()
        {
            var divida = NovaDivida(400m, 4);
            divida.DiaVencimento = 31;
            divida.DataPrimeiroVencimento = new DateTime(2027, 1, 31);

            await ExecutarAsync(divida);

            var parcelas = await ParcelasGravadasAsync();
            CollectionAssert.AreEqual(
                new[] { new DateTime(2027, 1, 31), new DateTime(2027, 2, 28), new DateTime(2027, 3, 31), new DateTime(2027, 4, 30) },
                parcelas.Select(p => p.DataVencimento).ToArray());
        }

        [TestMethod]
        public async Task ExecutarAsync_PrimeiraParcelaUsaDataPrimeiroVencimentoEAsDemaisODiaVencimento()
        {
            var divida = NovaDivida(300m, 3);
            divida.DiaVencimento = 10;
            divida.DataPrimeiroVencimento = new DateTime(2026, 10, 25, 14, 30, 0);

            await ExecutarAsync(divida);

            var parcelas = await ParcelasGravadasAsync();
            CollectionAssert.AreEqual(
                new[] { new DateTime(2026, 10, 25), new DateTime(2026, 11, 10), new DateTime(2026, 12, 10) },
                parcelas.Select(p => p.DataVencimento).ToArray());
        }

        [TestMethod]
        public async Task ExecutarAsync_DescreveParcelasComNomeENumero()
        {
            await ExecutarAsync(NovaDivida(300m, 3));

            var parcelas = await ParcelasGravadasAsync();
            CollectionAssert.AreEqual(new[] { "Notebook 1/3", "Notebook 2/3", "Notebook 3/3" }, parcelas.Select(p => p.Descricao).ToArray());
        }

        [TestMethod]
        public async Task ExecutarAsync_CortaNomeLongoParaCaberNaDescricao()
        {
            var divida = NovaDivida(1200m, 12);
            divida.Nome = new string('A', 100);

            await ExecutarAsync(divida);

            var parcelas = await ParcelasGravadasAsync();
            Assert.IsTrue(parcelas.All(p => p.Descricao.Length <= 100));
            Assert.EndsWith(" 12/12", parcelas[11].Descricao);
        }

        [TestMethod]
        public async Task ExecutarAsync_ReceitaSemNomeFalaEmReceita()
        {
            var receita = NovaDivida();
            receita.EhDivida = false;
            receita.Nome = " ";

            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(() => ExecutarAsync(receita));
            StringAssert.Contains(ex.Erros.Single(e => e.Campo == nameof(Divida.Nome)).Mensagem, "receita");
        }

        [TestMethod]
        public async Task ExecutarAsync_GeraParcelasTambemParaCredito()
        {
            var credito = NovaDivida(500m, 2);
            credito.EhDivida = false;

            var gravado = await ExecutarAsync(credito);

            await using var db = CriarContexto();
            Assert.IsFalse((await db.Dividas.SingleAsync()).EhDivida);
            Assert.AreEqual(2, await db.Parcelas.CountAsync(p => p.IdDivida == gravado.Id));
        }

        [TestMethod]
        public async Task ExecutarAsync_DescartaParcelasInformadasPeloChamador()
        {
            var divida = NovaDivida(200m, 2);
            divida.Parcelas.Add(new Parcela { Descricao = "Intrusa", Valor = 1m });

            await ExecutarAsync(divida);

            var parcelas = await ParcelasGravadasAsync();
            Assert.HasCount(2, parcelas);
            Assert.IsFalse(parcelas.Any(p => p.Descricao == "Intrusa"));
        }

        [TestMethod]
        public async Task ExecutarAsync_AceitaCredorDoUsuario()
        {
            var credor = new Credor { IdUsuario = _usuario.Id, Nome = "Loja" };
            await SemearAsync(credor);
            var divida = NovaDivida();
            divida.IdCredor = credor.Id;

            var gravada = await ExecutarAsync(divida);

            Assert.AreEqual(credor.Id, gravada.IdCredor);
        }

        [TestMethod]
        public async Task ExecutarAsync_GravaParaOUsuarioRecebidoERemoveEspacosDoNome()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var divida = NovaDivida();
            divida.IdUsuario = outro.Id;
            divida.Nome = "  Notebook  ";

            var gravada = await ExecutarAsync(divida);

            Assert.AreEqual(_usuario.Id, gravada.IdUsuario);
            Assert.AreEqual("Notebook", gravada.Nome);
            Assert.IsTrue(gravada.Ativo);
        }

        [TestMethod]
        [DataRow(0, DisplayName = "Dia 0")]
        [DataRow(32, DisplayName = "Dia 32")]
        public async Task ExecutarAsync_RecusaDiaVencimentoInvalido(int dia)
        {
            var divida = NovaDivida();
            divida.DiaVencimento = dia;

            await AssertRecusaCampoAsync(divida, nameof(Divida.DiaVencimento));
        }

        [TestMethod]
        [DataRow(0, DisplayName = "Zero parcelas")]
        [DataRow(361, DisplayName = "361 parcelas")]
        public async Task ExecutarAsync_RecusaNumeroDeParcelasForaDaFaixa(int parcelas)
        {
            await AssertRecusaCampoAsync(NovaDivida(parcelas: parcelas), nameof(Divida.NumeroParcelas));
        }

        [TestMethod]
        [DataRow(0.0, DisplayName = "Valor zero")]
        [DataRow(-10.0, DisplayName = "Valor negativo")]
        [DataRow(10.555, DisplayName = "Três casas decimais")]
        [DataRow(100000000.0, DisplayName = "Maior que decimal(10,2)")]
        public async Task ExecutarAsync_RecusaValorForaDoDominio(double valor)
        {
            await AssertRecusaCampoAsync(NovaDivida((decimal)valor), nameof(Divida.Valor));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaNomeVazio()
        {
            var divida = NovaDivida();
            divida.Nome = "   ";

            await AssertRecusaCampoAsync(divida, nameof(Divida.Nome));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaNomeMaiorQue100Caracteres()
        {
            var divida = NovaDivida();
            divida.Nome = new string('A', 101);

            await AssertRecusaCampoAsync(divida, nameof(Divida.Nome));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaSemConta()
        {
            var divida = NovaDivida();
            divida.IdConta = null;

            await AssertRecusaCampoAsync(divida, nameof(Divida.IdConta));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaSemCategoria()
        {
            var divida = NovaDivida();
            divida.IdCategoria = null;

            await AssertRecusaCampoAsync(divida, nameof(Divida.IdCategoria));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaContaDeOutroUsuario()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var contaDeOutro = new Conta { IdUsuario = outro.Id, Nome = "Alheia" };
            await SemearAsync(contaDeOutro);
            var divida = NovaDivida();
            divida.IdConta = contaDeOutro.Id;

            await AssertRecusaCampoAsync(divida, nameof(Divida.IdConta));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaCategoriaDesativada()
        {
            await using (var db = CriarContexto())
            {
                (await db.Categorias.SingleAsync()).Ativo = false;
                await db.SaveChangesAsync();
            }

            await AssertRecusaCampoAsync(NovaDivida(), nameof(Divida.IdCategoria));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaCredorDesativado()
        {
            var credor = new Credor { IdUsuario = _usuario.Id, Nome = "Loja", Ativo = false };
            await SemearAsync(credor);
            var divida = NovaDivida();
            divida.IdCredor = credor.Id;

            await AssertRecusaCampoAsync(divida, nameof(Divida.IdCredor));
        }

        [TestMethod]
        public async Task ExecutarAsync_MostraTodosOsErrosDeUmaVez()
        {
            var divida = NovaDivida(0m, 0);
            divida.Nome = "";
            divida.IdConta = null;

            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(() => ExecutarAsync(divida));

            CollectionAssert.AreEquivalent(
                new[] { nameof(Divida.Nome), nameof(Divida.Valor), nameof(Divida.NumeroParcelas), nameof(Divida.IdConta) },
                ex.Erros.Select(e => e.Campo).ToArray());
            await AssertNadaGravadoAsync();
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaUsuarioInexistente()
        {
            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(NovaDivida(), 999));
            await AssertNadaGravadoAsync();
        }
    }
}

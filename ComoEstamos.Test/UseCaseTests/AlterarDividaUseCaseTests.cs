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
    public sealed class AlterarDividaUseCaseTests : RepositoryTestBase
    {
        private AlterarDividaUseCase _useCase = null!;
        private CriarDividaUseCase _criar = null!;
        private Usuario _usuario = null!;
        private Conta _conta = null!;
        private Conta _outraConta = null!;
        private Categoria _categoria = null!;
        private Categoria _outraCategoria = null!;
        private Credor _credor = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            var uow = new UnitOfWorkFactory(ContextFactory);
            _useCase = new AlterarDividaUseCase(uow);
            _criar = new CriarDividaUseCase(uow);
            _usuario = await CriarUsuarioAsync();
            _conta = new Conta { IdUsuario = _usuario.Id, Nome = "Corrente" };
            _outraConta = new Conta { IdUsuario = _usuario.Id, Nome = "Poupança" };
            _categoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Compras" };
            _outraCategoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Eletrônicos" };
            _credor = new Credor { IdUsuario = _usuario.Id, Nome = "Loja" };
            await SemearAsync(_conta, _outraConta, _categoria, _outraCategoria, _credor);
        }

        /// <summary>Cria pela inclusão (R$ 1.200 em 12x, dia 10, a partir de 10/10/2026) e paga as <paramref name="pagas"/> primeiras.</summary>
        private async Task<Divida> CriarAsync(int pagas = 0, decimal valor = 1200m, int parcelas = 12)
        {
            var divida = await _criar.ExecutarAsync(new Divida
            {
                IdConta = _conta.Id,
                IdCategoria = _categoria.Id,
                Nome = "Notebook",
                Valor = valor,
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

        /// <summary>Cópia do que está gravado, com os campos editáveis, para alterar.</summary>
        private static Divida Edicao(Divida divida) => new()
        {
            Id = divida.Id,
            Nome = divida.Nome,
            IdCredor = divida.IdCredor,
            IdConta = divida.IdConta,
            IdCategoria = divida.IdCategoria,
            Valor = divida.Valor,
            NumeroParcelas = divida.NumeroParcelas,
            DiaVencimento = divida.DiaVencimento,
            DataPrimeiroVencimento = divida.DataPrimeiroVencimento
        };

        private Task ExecutarAsync(Divida divida) => _useCase.ExecutarAsync(divida, _usuario.Id);

        private async Task<Divida> DividaGravadaAsync()
        {
            await using var db = CriarContexto();
            return await db.Dividas.AsNoTracking().SingleAsync();
        }

        private async Task<List<Parcela>> ParcelasAtivasAsync()
        {
            await using var db = CriarContexto();
            return await db.Parcelas.AsNoTracking().Where(p => p.Ativo).OrderBy(p => p.DataVencimento).ToListAsync();
        }

        /// <summary>Retrato do banco para conferir que uma alteração recusada não gravou nada.</summary>
        private async Task<string> RetratoAsync()
        {
            await using var db = CriarContexto();
            var divida = await db.Dividas.AsNoTracking().SingleAsync();
            var parcelas = await db.Parcelas.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
            return $"{divida.Nome}|{divida.Valor}|{divida.NumeroParcelas}|{divida.DiaVencimento}|{divida.DataPrimeiroVencimento:d}|{divida.IdConta}|" +
                string.Join(";", parcelas.Select(p => $"{p.Id},{p.Ativo},{p.Valor},{p.DataVencimento:d},{p.Descricao},{p.IdConta}"));
        }

        private async Task AssertRecusaCampoAsync(Divida edicao, string campo)
        {
            var antes = await RetratoAsync();
            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(() => ExecutarAsync(edicao));
            Assert.IsTrue(ex.Erros.Any(e => e.Campo == campo), $"Esperava erro no campo {campo}: {ex.Message}");
            Assert.AreEqual(antes, await RetratoAsync());
        }

        // ---- Campos simples: as parcelas continuam as mesmas ----

        [TestMethod]
        public async Task ExecutarAsync_CamposSimplesMantemAsParcelasERenomeiaADescricao()
        {
            var divida = await CriarAsync(pagas: 2);
            var idsAntes = (await ParcelasAtivasAsync()).Select(p => p.Id).ToArray();
            var edicao = Edicao(divida);
            edicao.Nome = "  Notebook Dell  ";
            edicao.IdCredor = _credor.Id;

            await ExecutarAsync(edicao);

            var gravada = await DividaGravadaAsync();
            Assert.AreEqual("Notebook Dell", gravada.Nome);
            Assert.AreEqual(_credor.Id, gravada.IdCredor);
            var parcelas = await ParcelasAtivasAsync();
            CollectionAssert.AreEqual(idsAntes, parcelas.Select(p => p.Id).ToArray());
            Assert.AreEqual("Notebook Dell 1/12", parcelas[0].Descricao);
            Assert.AreEqual("Notebook Dell 12/12", parcelas[11].Descricao);
        }

        [TestMethod]
        public async Task ExecutarAsync_NovaContaECategoriaValemSoParaAsParcelasEmAberto()
        {
            var divida = await CriarAsync(pagas: 2);
            var edicao = Edicao(divida);
            edicao.IdConta = _outraConta.Id;
            edicao.IdCategoria = _outraCategoria.Id;

            await ExecutarAsync(edicao);

            var parcelas = await ParcelasAtivasAsync();
            Assert.IsTrue(parcelas.Take(2).All(p => p.IdConta == _conta.Id && p.IdCategoria == _categoria.Id));
            Assert.IsTrue(parcelas.Skip(2).All(p => p.IdConta == _outraConta.Id && p.IdCategoria == _outraCategoria.Id));
            Assert.AreEqual(_outraConta.Id, (await DividaGravadaAsync()).IdConta);
        }

        // ---- Campos financeiros: as parcelas em aberto são refeitas ----

        [TestMethod]
        public async Task ExecutarAsync_SemPagasRefazTodasAsParcelas()
        {
            var divida = await CriarAsync();
            var edicao = Edicao(divida);
            edicao.Valor = 100m;
            edicao.NumeroParcelas = 3;
            edicao.DiaVencimento = 31;
            edicao.DataPrimeiroVencimento = new DateTime(2027, 1, 31);

            await ExecutarAsync(edicao);

            var parcelas = await ParcelasAtivasAsync();
            CollectionAssert.AreEqual(new[] { 33.33m, 33.33m, 33.34m }, parcelas.Select(p => p.Valor).ToArray());
            CollectionAssert.AreEqual(
                new[] { new DateTime(2027, 1, 31), new DateTime(2027, 2, 28), new DateTime(2027, 3, 31) },
                parcelas.Select(p => p.DataVencimento).ToArray());
            CollectionAssert.AreEqual(new[] { "Notebook 1/3", "Notebook 2/3", "Notebook 3/3" }, parcelas.Select(p => p.Descricao).ToArray());
            Assert.IsTrue(parcelas.All(p => p.IdDivida == divida.Id && p.DataPagamento is null));
            await using var db = CriarContexto();
            Assert.AreEqual(12, await db.Parcelas.CountAsync(p => !p.Ativo), "As 12 antigas ficam desativadas, não apagadas.");
        }

        [TestMethod]
        public async Task ExecutarAsync_ComPagasMantemAsPagasEDivideOQueFaltaPagar()
        {
            // 12x de R$ 100, duas pagas (R$ 200). Novo: R$ 1.000 em 10x → faltam R$ 800 em 8x de R$ 100.
            var divida = await CriarAsync(pagas: 2);
            var pagasAntes = (await ParcelasAtivasAsync()).Take(2).Select(p => p.Id).ToArray();
            var edicao = Edicao(divida);
            edicao.Valor = 1000m;
            edicao.NumeroParcelas = 10;

            await ExecutarAsync(edicao);

            var parcelas = await ParcelasAtivasAsync();
            Assert.HasCount(10, parcelas);
            CollectionAssert.AreEqual(pagasAntes, parcelas.Take(2).Select(p => p.Id).ToArray());
            Assert.IsTrue(parcelas.Take(2).All(p => p.DataPagamento is not null && p.Valor == 100m));
            Assert.IsTrue(parcelas.Skip(2).All(p => p.DataPagamento is null && p.Valor == 100m));
            Assert.AreEqual(1000m, parcelas.Sum(p => p.Valor));
            Assert.AreEqual("Notebook 1/10", parcelas[0].Descricao);
            Assert.AreEqual("Notebook 3/10", parcelas[2].Descricao);
            Assert.AreEqual(new DateTime(2026, 12, 10), parcelas[2].DataVencimento);
            Assert.AreEqual(new DateTime(2027, 7, 10), parcelas[9].DataVencimento);
        }

        [TestMethod]
        public async Task ExecutarAsync_ComPagasColocaOsCentavosNaUltimaParcela()
        {
            var divida = await CriarAsync(pagas: 1, valor: 300m, parcelas: 3);
            var edicao = Edicao(divida);
            edicao.Valor = 300.01m;
            edicao.NumeroParcelas = 4;

            await ExecutarAsync(edicao);

            // Pago R$ 100; faltam R$ 200,01 em 3x → 66,67 · 66,67 · 66,67.
            CollectionAssert.AreEqual(new[] { 100m, 66.67m, 66.67m, 66.67m }, (await ParcelasAtivasAsync()).Select(p => p.Valor).ToArray());
        }

        [TestMethod]
        public async Task ExecutarAsync_ComPagasAceitaMudarODiaDeVencimento()
        {
            var divida = await CriarAsync(pagas: 1, valor: 300m, parcelas: 3);
            var edicao = Edicao(divida);
            edicao.DiaVencimento = 20;

            await ExecutarAsync(edicao);

            CollectionAssert.AreEqual(
                new[] { new DateTime(2026, 10, 10), new DateTime(2026, 11, 20), new DateTime(2026, 12, 20) },
                (await ParcelasAtivasAsync()).Select(p => p.DataVencimento).ToArray());
        }

        [TestMethod]
        public async Task ExecutarAsync_TodasPagasAceitaValorIgualAoPago()
        {
            var divida = await CriarAsync(pagas: 3, valor: 300m, parcelas: 3);
            var edicao = Edicao(divida);
            edicao.DiaVencimento = 15;

            await ExecutarAsync(edicao);

            var parcelas = await ParcelasAtivasAsync();
            Assert.HasCount(3, parcelas);
            Assert.IsTrue(parcelas.All(p => p.DataPagamento is not null));
        }

        // ---- Recusas ----

        [TestMethod]
        public async Task ExecutarAsync_ComPagasRecusaMudarOPrimeiroVencimento()
        {
            var edicao = Edicao(await CriarAsync(pagas: 1));
            edicao.DataPrimeiroVencimento = new DateTime(2026, 11, 10);

            await AssertRecusaCampoAsync(edicao, nameof(Divida.DataPrimeiroVencimento));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaMenosParcelasQueAsPagas()
        {
            var edicao = Edicao(await CriarAsync(pagas: 3));
            edicao.NumeroParcelas = 2;
            edicao.Valor = 300m;

            await AssertRecusaCampoAsync(edicao, nameof(Divida.NumeroParcelas));
        }

        [TestMethod]
        [DataRow(200.0, DisplayName = "Menor que o pago")]
        [DataRow(300.0, DisplayName = "Igual ao pago, com parcelas em aberto")]
        [DataRow(300.08, DisplayName = "Menos de 1 centavo por parcela em aberto")]
        public async Task ExecutarAsync_RecusaValorQueNaoCobreOJaPago(double valor)
        {
            // R$ 300 já pagos; 12 parcelas → 9 em aberto.
            var edicao = Edicao(await CriarAsync(pagas: 3));
            edicao.Valor = (decimal)valor;

            await AssertRecusaCampoAsync(edicao, nameof(Divida.Valor));
        }

        [TestMethod]
        public async Task ExecutarAsync_TodasPagasRecusaValorDiferenteDoPago()
        {
            var edicao = Edicao(await CriarAsync(pagas: 3, valor: 300m, parcelas: 3));
            edicao.Valor = 350m;

            await AssertRecusaCampoAsync(edicao, nameof(Divida.Valor));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaDominioComoNaInclusao()
        {
            var edicao = Edicao(await CriarAsync());
            edicao.Nome = " ";
            edicao.IdConta = null;

            await AssertRecusaCampoAsync(edicao, nameof(Divida.Nome));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaCategoriaDeOutroUsuario()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var alheia = new Categoria { IdUsuario = outro.Id, Nome = "Alheia" };
            await SemearAsync(alheia);
            var edicao = Edicao(await CriarAsync());
            edicao.IdCategoria = alheia.Id;

            await AssertRecusaCampoAsync(edicao, nameof(Divida.IdCategoria));
        }

        [TestMethod]
        public async Task ExecutarAsync_InexistenteOuDeOutroUsuarioLancaKeyNotFound()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var divida = await CriarAsync();
            var inexistente = Edicao(divida);
            inexistente.Id = 999;

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => ExecutarAsync(inexistente));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync(Edicao(divida), outro.Id));
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaDividaExcluida()
        {
            var divida = await CriarAsync();
            await using (var db = CriarContexto())
            {
                (await db.Dividas.SingleAsync()).Ativo = false;
                await db.SaveChangesAsync();
            }

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => ExecutarAsync(Edicao(divida)));
        }
    }
}

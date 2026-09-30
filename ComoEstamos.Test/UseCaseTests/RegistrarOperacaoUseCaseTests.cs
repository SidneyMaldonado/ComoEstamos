using ComoEstamos.Core;
using ComoEstamos.Core.UseCases;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    [TestClass]
    public sealed class RegistrarOperacaoUseCaseTests : RepositoryTestBase
    {
        private RegistrarOperacaoUseCase _useCase = null!;
        private Investimento _investimento = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _useCase = new RegistrarOperacaoUseCase(new UnitOfWorkFactory(ContextFactory));
            var usuario = await CriarUsuarioAsync();
            var carteira = new Carteira { IdUsuario = usuario.Id, Nome = "Ações" };
            await SemearAsync(carteira);
            _investimento = new Investimento { IdCarteira = carteira.Id, Nome = "PETR4", Quantidade = 100m, Cotacao = 32m };
            await SemearAsync(_investimento);
        }

        private Operacao NovaOperacao(bool ehCompra, decimal quantidade) => new()
        {
            IdInvestimento = _investimento.Id,
            EhCompra = ehCompra,
            Quantidade = quantidade,
            Valor = quantidade * 32m,
            DataOperacao = new DateTime(2026, 9, 30)
        };

        private async Task<decimal> QuantidadeGravadaAsync()
        {
            await using var db = CriarContexto();
            return (await db.Investimentos.SingleAsync()).Quantidade;
        }

        private async Task<int> OperacoesGravadasAsync()
        {
            await using var db = CriarContexto();
            return await db.Operacoes.CountAsync();
        }

        [TestMethod]
        public async Task ExecutarAsync_CompraGravaOperacaoESomaQuantidade()
        {
            var operacao = await _useCase.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 50m));

            Assert.IsGreaterThan(0, operacao.Id);
            Assert.AreEqual(150m, await QuantidadeGravadaAsync());
            Assert.AreEqual(1, await OperacoesGravadasAsync());
        }

        [TestMethod]
        public async Task ExecutarAsync_VendaGravaOperacaoESubtraiQuantidade()
        {
            await _useCase.ExecutarAsync(NovaOperacao(ehCompra: false, quantidade: 30m));

            Assert.AreEqual(70m, await QuantidadeGravadaAsync());
            Assert.AreEqual(1, await OperacoesGravadasAsync());
        }

        [TestMethod]
        public async Task ExecutarAsync_PermiteVenderTodaAQuantidade()
        {
            await _useCase.ExecutarAsync(NovaOperacao(ehCompra: false, quantidade: 100m));

            Assert.AreEqual(0m, await QuantidadeGravadaAsync());
        }

        [TestMethod]
        public async Task ExecutarAsync_AceitaQuantidadeFracionada()
        {
            await _useCase.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 0.000123m));

            Assert.AreEqual(100.000123m, await QuantidadeGravadaAsync());
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaVendaMaiorQueADisponivelSemGravarNada()
        {
            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => _useCase.ExecutarAsync(NovaOperacao(ehCompra: false, quantidade: 100.5m)));

            Assert.AreEqual(100m, await QuantidadeGravadaAsync());
            Assert.AreEqual(0, await OperacoesGravadasAsync());
        }

        [TestMethod]
        [DataRow(0.0, DisplayName = "Quantidade zero")]
        [DataRow(-1.0, DisplayName = "Quantidade negativa")]
        public async Task ExecutarAsync_RecusaQuantidadeNaoPositiva(double quantidade)
        {
            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => _useCase.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: (decimal)quantidade)));

            Assert.AreEqual(0, await OperacoesGravadasAsync());
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaValorNegativo()
        {
            var operacao = NovaOperacao(ehCompra: true, quantidade: 10m);
            operacao.Valor = -1m;

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _useCase.ExecutarAsync(operacao));

            Assert.AreEqual(0, await OperacoesGravadasAsync());
        }

        [TestMethod]
        public async Task ExecutarAsync_RecusaInvestimentoDesativado()
        {
            await using (var db = CriarContexto())
            {
                (await db.Investimentos.SingleAsync()).Ativo = false;
                await db.SaveChangesAsync();
            }

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => _useCase.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 10m)));

            Assert.AreEqual(100m, await QuantidadeGravadaAsync());
            Assert.AreEqual(0, await OperacoesGravadasAsync());
        }

        [TestMethod]
        public async Task ExecutarAsync_LancaExcecaoQuandoInvestimentoNaoExiste()
        {
            var operacao = NovaOperacao(ehCompra: true, quantidade: 10m);
            operacao.IdInvestimento = 999;

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _useCase.ExecutarAsync(operacao));
        }
    }
}

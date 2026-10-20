using ComoEstamos.Core;
using ComoEstamos.Core.UseCases.Operacoes;
using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    /// <summary>Casos de uso de operação: registrar (compra e venda), excluir e buscar as últimas.</summary>
    [TestClass]
    public sealed class OperacaoUseCaseTests : RepositoryTestBase
    {
        private RegistrarOperacaoUseCase _registrar = null!;
        private ExcluirOperacaoUseCase _excluir = null!;
        private BuscarUltimasOperacoesUseCase _buscarUltimas = null!;
        private Usuario _usuario = null!;
        private Usuario _outroUsuario = null!;
        private Investimento _investimento = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            var uow = new UnitOfWorkFactory(ContextFactory);
            _registrar = new RegistrarOperacaoUseCase(uow);
            _excluir = new ExcluirOperacaoUseCase(uow);
            _buscarUltimas = new BuscarUltimasOperacoesUseCase(uow);
            _usuario = await CriarUsuarioAsync();
            _outroUsuario = await CriarUsuarioAsync("outro@teste.com");
            var carteira = new Carteira { IdUsuario = _usuario.Id, Nome = "Ações" };
            await SemearAsync(carteira);
            _investimento = new Investimento { IdCarteira = carteira.Id, Nome = "PETR4", Quantidade = 100m, Cotacao = 32m };
            await SemearAsync(_investimento);
        }

        private Operacao NovaOperacao(bool ehCompra, decimal quantidade, DateTime? data = null) => new()
        {
            IdInvestimento = _investimento.Id,
            EhCompra = ehCompra,
            Quantidade = quantidade,
            ValorTotal = decimal.Round(quantidade * 32m, 2),
            DataOperacao = data ?? new DateTime(2026, 9, 30, 14, 35, 0)
        };

        private async Task<decimal> QuantidadeGravadaAsync()
        {
            await using var db = CriarContexto();
            return (await db.Investimentos.SingleAsync()).Quantidade;
        }

        private async Task<int> OperacoesAtivasAsync()
        {
            await using var db = CriarContexto();
            return await db.Operacoes.CountAsync(o => o.Ativo);
        }

        private static void AssertErroNoCampo(ValidacaoException ex, string campo) =>
            Assert.IsTrue(ex.Erros.Any(e => e.Campo == campo), $"Esperado erro no campo {campo}.");

        // Registrar

        [TestMethod]
        public async Task Registrar_CompraGravaOperacaoESomaQuantidade()
        {
            var operacao = await _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 50m), _usuario.Id);

            Assert.IsGreaterThan(0, operacao.Id);
            Assert.AreEqual(150m, await QuantidadeGravadaAsync());
            Assert.AreEqual(1, await OperacoesAtivasAsync());
        }

        [TestMethod]
        public async Task Registrar_VendaGravaOperacaoESubtraiQuantidade()
        {
            await _registrar.ExecutarAsync(NovaOperacao(ehCompra: false, quantidade: 30m), _usuario.Id);

            Assert.AreEqual(70m, await QuantidadeGravadaAsync());
            Assert.AreEqual(1, await OperacoesAtivasAsync());
        }

        [TestMethod]
        public async Task Registrar_GravaDataEHora()
        {
            var operacao = await _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 1m), _usuario.Id);

            await using var db = CriarContexto();
            Assert.AreEqual(new DateTime(2026, 9, 30, 14, 35, 0), (await db.Operacoes.SingleAsync(o => o.Id == operacao.Id)).DataOperacao);
        }

        [TestMethod]
        public async Task Registrar_PermiteVenderTodaAQuantidade()
        {
            await _registrar.ExecutarAsync(NovaOperacao(ehCompra: false, quantidade: 100m), _usuario.Id);

            Assert.AreEqual(0m, await QuantidadeGravadaAsync());
        }

        [TestMethod]
        public async Task Registrar_AceitaQuantidadeFracionada()
        {
            await _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 0.000123m), _usuario.Id);

            Assert.AreEqual(100.000123m, await QuantidadeGravadaAsync());
        }

        [TestMethod]
        public async Task Registrar_AceitaValorZero()
        {
            var operacao = NovaOperacao(ehCompra: true, quantidade: 10m);
            operacao.ValorTotal = 0m;

            await _registrar.ExecutarAsync(operacao, _usuario.Id);

            Assert.AreEqual(1, await OperacoesAtivasAsync());
        }

        [TestMethod]
        public async Task Registrar_RecusaVendaMaiorQueADisponivelSemGravarNada()
        {
            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(
                () => _registrar.ExecutarAsync(NovaOperacao(ehCompra: false, quantidade: 100.5m), _usuario.Id));

            AssertErroNoCampo(ex, nameof(Operacao.Quantidade));
            Assert.AreEqual(100m, await QuantidadeGravadaAsync());
            Assert.AreEqual(0, await OperacoesAtivasAsync());
        }

        [TestMethod]
        [DataRow(0.0, DisplayName = "Quantidade zero")]
        [DataRow(-1.0, DisplayName = "Quantidade negativa")]
        [DataRow(0.0000001, DisplayName = "Mais de 6 casas")]
        public async Task Registrar_RecusaQuantidadeForaDoDominio(double quantidade)
        {
            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(
                () => _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: (decimal)quantidade), _usuario.Id));

            AssertErroNoCampo(ex, nameof(Operacao.Quantidade));
            Assert.AreEqual(0, await OperacoesAtivasAsync());
        }

        [TestMethod]
        [DataRow(-1.0, DisplayName = "Valor negativo")]
        [DataRow(10.001, DisplayName = "Mais de 2 casas")]
        public async Task Registrar_RecusaValorForaDoDominio(double valor)
        {
            var operacao = NovaOperacao(ehCompra: true, quantidade: 10m);
            operacao.ValorTotal = (decimal)valor;

            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(() => _registrar.ExecutarAsync(operacao, _usuario.Id));

            AssertErroNoCampo(ex, nameof(Operacao.ValorTotal));
            Assert.AreEqual(0, await OperacoesAtivasAsync());
        }

        [TestMethod]
        public async Task Registrar_RecusaDataFutura()
        {
            var operacao = NovaOperacao(ehCompra: true, quantidade: 10m, data: DateTime.Now.AddMinutes(10));

            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(() => _registrar.ExecutarAsync(operacao, _usuario.Id));

            AssertErroNoCampo(ex, nameof(Operacao.DataOperacao));
            Assert.AreEqual(100m, await QuantidadeGravadaAsync());
        }

        [TestMethod]
        public async Task Registrar_RecusaInvestimentoDesativado()
        {
            await using (var db = CriarContexto())
            {
                (await db.Investimentos.SingleAsync()).Ativo = false;
                await db.SaveChangesAsync();
            }

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 10m), _usuario.Id));

            Assert.AreEqual(100m, await QuantidadeGravadaAsync());
            Assert.AreEqual(0, await OperacoesAtivasAsync());
        }

        [TestMethod]
        public async Task Registrar_InvestimentoInexistenteOuDeOutroUsuario_LancaKeyNotFound()
        {
            var inexistente = NovaOperacao(ehCompra: true, quantidade: 10m);
            inexistente.IdInvestimento = 999;

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _registrar.ExecutarAsync(inexistente, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
                () => _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 10m), _outroUsuario.Id));
            Assert.AreEqual(0, await OperacoesAtivasAsync());
        }

        [TestMethod]
        public async Task Registrar_GravaSoOsCamposDoFormulario()
        {
            var operacao = NovaOperacao(ehCompra: true, quantidade: 10m);
            operacao.Ativo = false;

            var gravada = await _registrar.ExecutarAsync(operacao, _usuario.Id);

            Assert.IsTrue(gravada.Ativo);
            Assert.AreEqual(1, await OperacoesAtivasAsync());
        }

        // Excluir

        [TestMethod]
        public async Task Excluir_CompraDesativaESubtraiQuantidade()
        {
            var compra = await _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 50m), _usuario.Id);

            await _excluir.ExecutarAsync(compra.Id, _usuario.Id);

            Assert.AreEqual(100m, await QuantidadeGravadaAsync());
            Assert.AreEqual(0, await OperacoesAtivasAsync());
        }

        [TestMethod]
        public async Task Excluir_VendaDesativaESomaQuantidade()
        {
            var venda = await _registrar.ExecutarAsync(NovaOperacao(ehCompra: false, quantidade: 30m), _usuario.Id);

            await _excluir.ExecutarAsync(venda.Id, _usuario.Id);

            Assert.AreEqual(100m, await QuantidadeGravadaAsync());
            Assert.AreEqual(0, await OperacoesAtivasAsync());
        }

        [TestMethod]
        public async Task Excluir_RecusaQuandoAQuantidadeFicariaNegativa()
        {
            var compra = await _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 50m), _usuario.Id);
            await _registrar.ExecutarAsync(NovaOperacao(ehCompra: false, quantidade: 120m), _usuario.Id);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _excluir.ExecutarAsync(compra.Id, _usuario.Id));

            Assert.AreEqual(30m, await QuantidadeGravadaAsync());
            Assert.AreEqual(2, await OperacoesAtivasAsync());
        }

        [TestMethod]
        public async Task Excluir_RecusaOperacaoJaExcluida()
        {
            var compra = await _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 50m), _usuario.Id);
            await _excluir.ExecutarAsync(compra.Id, _usuario.Id);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => _excluir.ExecutarAsync(compra.Id, _usuario.Id));

            Assert.AreEqual(100m, await QuantidadeGravadaAsync());
        }

        [TestMethod]
        public async Task Excluir_InexistenteOuDeOutroUsuario_LancaKeyNotFound()
        {
            var compra = await _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 50m), _usuario.Id);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _excluir.ExecutarAsync(999, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _excluir.ExecutarAsync(compra.Id, _outroUsuario.Id));
            Assert.AreEqual(1, await OperacoesAtivasAsync());
        }

        // Buscar as últimas

        [TestMethod]
        public async Task BuscarUltimas_TrazAsMaisRecentesAteOLimite()
        {
            for (var dia = 1; dia <= 12; dia++)
                await _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 1m, data: new DateTime(2026, 9, dia, 10, 0, 0)), _usuario.Id);

            var lista = await _buscarUltimas.ExecutarAsync(_investimento.Id, 10, _usuario.Id);

            Assert.HasCount(10, lista);
            Assert.AreEqual(new DateTime(2026, 9, 12, 10, 0, 0), lista[0].DataOperacao);
            Assert.AreEqual(new DateTime(2026, 9, 3, 10, 0, 0), lista[^1].DataOperacao);
        }

        [TestMethod]
        public async Task BuscarUltimas_IgnoraExcluidas()
        {
            var compra = await _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 1m), _usuario.Id);
            await _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 2m), _usuario.Id);
            await _excluir.ExecutarAsync(compra.Id, _usuario.Id);

            var lista = await _buscarUltimas.ExecutarAsync(_investimento.Id, 10, _usuario.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual(2m, lista[0].Quantidade);
        }

        [TestMethod]
        public async Task BuscarUltimas_VaziaParaOutroUsuarioOuInvestimentoInexistente()
        {
            await _registrar.ExecutarAsync(NovaOperacao(ehCompra: true, quantidade: 1m), _usuario.Id);

            Assert.IsEmpty(await _buscarUltimas.ExecutarAsync(_investimento.Id, 10, _outroUsuario.Id));
            Assert.IsEmpty(await _buscarUltimas.ExecutarAsync(999, 10, _usuario.Id));
        }
    }
}

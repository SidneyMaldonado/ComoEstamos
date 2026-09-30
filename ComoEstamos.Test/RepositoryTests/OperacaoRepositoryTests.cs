using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;

namespace ComoEstamos.Test.RepositoryTests
{
    [TestClass]
    public sealed class OperacaoRepositoryTests : RepositoryTestBase
    {
        private OperacaoRepository _repository = null!;
        private Carteira _carteira = null!;
        private Investimento _investimento = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _repository = new OperacaoRepository(ContextFactory);
            var usuario = await CriarUsuarioAsync();
            _carteira = new Carteira { IdUsuario = usuario.Id, Nome = "Ações" };
            await SemearAsync(_carteira);
            _investimento = new Investimento { IdCarteira = _carteira.Id, Nome = "PETR4" };
            await SemearAsync(_investimento);
        }

        private Operacao NovaOperacao(DateTime data, bool ehCompra = true, int? idInvestimento = null) => new()
        {
            IdInvestimento = idInvestimento ?? _investimento.Id,
            EhCompra = ehCompra,
            DataOperacao = data,
            Quantidade = 10m,
            Valor = 325m
        };

        [TestMethod]
        public async Task InserirAsync_PersisteCompra()
        {
            var operacao = await _repository.InserirAsync(NovaOperacao(new DateTime(2026, 9, 1)));

            var obtida = await _repository.ObterPorIdAsync(operacao.Id);
            Assert.IsNotNull(obtida);
            Assert.IsTrue(obtida.EhCompra);
            Assert.AreEqual(new DateTime(2026, 9, 1), obtida.DataOperacao);
            Assert.AreEqual(10m, obtida.Quantidade);
            Assert.AreEqual(325m, obtida.Valor);
        }

        [TestMethod]
        public async Task InserirAsync_PersisteVenda()
        {
            var operacao = await _repository.InserirAsync(NovaOperacao(new DateTime(2026, 9, 1), ehCompra: false));

            var obtida = await _repository.ObterPorIdAsync(operacao.Id);
            Assert.IsNotNull(obtida);
            Assert.IsFalse(obtida.EhCompra);
        }

        [TestMethod]
        public async Task ListarPorInvestimentoAsync_RetornaSomenteOperacoesDoInvestimento()
        {
            var outro = new Investimento { IdCarteira = _carteira.Id, Nome = "VALE3" };
            await SemearAsync(outro);
            await SemearAsync(
                NovaOperacao(new DateTime(2026, 9, 1)),
                NovaOperacao(new DateTime(2026, 9, 2), idInvestimento: outro.Id));

            var lista = await _repository.ListarPorInvestimentoAsync(_investimento.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual(_investimento.Id, lista[0].IdInvestimento);
        }

        [TestMethod]
        public async Task ListarPorInvestimentoAsync_OrdenaDaMaisRecenteParaMaisAntiga()
        {
            await SemearAsync(
                NovaOperacao(new DateTime(2026, 8, 1)),
                NovaOperacao(new DateTime(2026, 9, 15)),
                NovaOperacao(new DateTime(2026, 9, 1)));

            var lista = await _repository.ListarPorInvestimentoAsync(_investimento.Id);

            CollectionAssert.AreEqual(
                new[] { new DateTime(2026, 9, 15), new DateTime(2026, 9, 1), new DateTime(2026, 8, 1) },
                lista.Select(o => o.DataOperacao).ToArray());
        }

        [TestMethod]
        public async Task ListarPorInvestimentoAsync_IgnoraInativasPorPadrao()
        {
            var inativa = NovaOperacao(new DateTime(2026, 9, 2));
            inativa.Ativo = false;
            await SemearAsync(NovaOperacao(new DateTime(2026, 9, 1)), inativa);

            var lista = await _repository.ListarPorInvestimentoAsync(_investimento.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual(new DateTime(2026, 9, 1), lista[0].DataOperacao);
        }

        [TestMethod]
        public async Task ListarPorInvestimentoAsync_IncluiInativasQuandoSolicitado()
        {
            var inativa = NovaOperacao(new DateTime(2026, 9, 2));
            inativa.Ativo = false;
            await SemearAsync(NovaOperacao(new DateTime(2026, 9, 1)), inativa);

            var lista = await _repository.ListarPorInvestimentoAsync(_investimento.Id, incluirInativos: true);

            Assert.HasCount(2, lista);
        }

        [TestMethod]
        public async Task AtualizarAsync_AlteraQuantidadeEValor()
        {
            var operacao = await _repository.InserirAsync(NovaOperacao(new DateTime(2026, 9, 1)));

            operacao.Quantidade = 20m;
            operacao.Valor = 650m;
            await _repository.AtualizarAsync(operacao);

            var obtida = await _repository.ObterPorIdAsync(operacao.Id);
            Assert.IsNotNull(obtida);
            Assert.AreEqual(20m, obtida.Quantidade);
            Assert.AreEqual(650m, obtida.Valor);
        }

        [TestMethod]
        public async Task DesativarAsync_RemoveDaListagemDoInvestimento()
        {
            var operacao = await _repository.InserirAsync(NovaOperacao(new DateTime(2026, 9, 1)));

            await _repository.DesativarAsync(operacao.Id);

            Assert.IsEmpty(await _repository.ListarPorInvestimentoAsync(_investimento.Id));
        }
    }
}

using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;

namespace ComoEstamos.Test.RepositoryTests
{
    [TestClass]
    public sealed class HistoricoRepositoryTests : RepositoryTestBase
    {
        private HistoricoRepository _repository = null!;
        private Carteira _carteira = null!;
        private Investimento _investimento = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _repository = new HistoricoRepository(ContextFactory);
            var usuario = await CriarUsuarioAsync();
            _carteira = new Carteira { IdUsuario = usuario.Id, Nome = "Ações" };
            await SemearAsync(_carteira);
            _investimento = new Investimento { IdCarteira = _carteira.Id, Nome = "PETR4" };
            await SemearAsync(_investimento);
        }

        private Historico NovoHistorico(DateTime data, int? idInvestimento = null) => new()
        {
            IdInvestimento = idInvestimento ?? _investimento.Id,
            DataHistorico = data,
            NomeInvestimento = "PETR4",
            Quantidade = 100m,
            Cotacao = 32.5m
        };

        [TestMethod]
        public async Task InserirAsync_PersisteRetratoDoInvestimento()
        {
            var historico = NovoHistorico(new DateTime(2026, 9, 30));
            historico.Quantidade = 150.123456m;
            historico.Cotacao = 38.654321m;
            historico.Observacao = "Fechamento do mês";

            await _repository.InserirAsync(historico);

            var obtido = await _repository.ObterPorIdAsync(historico.Id);
            Assert.IsNotNull(obtido);
            Assert.AreEqual(new DateTime(2026, 9, 30), obtido.DataHistorico);
            Assert.AreEqual("PETR4", obtido.NomeInvestimento);
            Assert.AreEqual(150.123456m, obtido.Quantidade);
            Assert.AreEqual(38.654321m, obtido.Cotacao);
            Assert.AreEqual("Fechamento do mês", obtido.Observacao);
        }

        [TestMethod]
        public async Task ListarPorInvestimentoAsync_RetornaSomenteHistoricosDoInvestimento()
        {
            var outro = new Investimento { IdCarteira = _carteira.Id, Nome = "VALE3" };
            await SemearAsync(outro);
            await SemearAsync(
                NovoHistorico(new DateTime(2026, 9, 1)),
                NovoHistorico(new DateTime(2026, 9, 2), outro.Id));

            var lista = await _repository.ListarPorInvestimentoAsync(_investimento.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual(_investimento.Id, lista[0].IdInvestimento);
        }

        [TestMethod]
        public async Task ListarPorInvestimentoAsync_OrdenaDoMaisRecenteParaMaisAntigo()
        {
            await SemearAsync(
                NovoHistorico(new DateTime(2026, 7, 31)),
                NovoHistorico(new DateTime(2026, 9, 30)),
                NovoHistorico(new DateTime(2026, 8, 31)));

            var lista = await _repository.ListarPorInvestimentoAsync(_investimento.Id);

            CollectionAssert.AreEqual(
                new[] { new DateTime(2026, 9, 30), new DateTime(2026, 8, 31), new DateTime(2026, 7, 31) },
                lista.Select(h => h.DataHistorico).ToArray());
        }

        [TestMethod]
        public async Task ListarPorInvestimentoAsync_IgnoraInativosPorPadrao()
        {
            var inativo = NovoHistorico(new DateTime(2026, 9, 2));
            inativo.Ativo = false;
            await SemearAsync(NovoHistorico(new DateTime(2026, 9, 1)), inativo);

            var lista = await _repository.ListarPorInvestimentoAsync(_investimento.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual(new DateTime(2026, 9, 1), lista[0].DataHistorico);
        }

        [TestMethod]
        public async Task ListarPorInvestimentoAsync_IncluiInativosQuandoSolicitado()
        {
            var inativo = NovoHistorico(new DateTime(2026, 9, 2));
            inativo.Ativo = false;
            await SemearAsync(NovoHistorico(new DateTime(2026, 9, 1)), inativo);

            var lista = await _repository.ListarPorInvestimentoAsync(_investimento.Id, incluirInativos: true);

            Assert.HasCount(2, lista);
        }

        [TestMethod]
        public async Task AtualizarAsync_AlteraObservacao()
        {
            var historico = await _repository.InserirAsync(NovoHistorico(new DateTime(2026, 9, 30)));

            historico.Observacao = "Corrigido";
            await _repository.AtualizarAsync(historico);

            var obtido = await _repository.ObterPorIdAsync(historico.Id);
            Assert.AreEqual("Corrigido", obtido?.Observacao);
        }

        [TestMethod]
        public async Task DesativarAsync_RemoveDaListagemDoInvestimento()
        {
            var historico = await _repository.InserirAsync(NovoHistorico(new DateTime(2026, 9, 30)));

            await _repository.DesativarAsync(historico.Id);

            Assert.IsEmpty(await _repository.ListarPorInvestimentoAsync(_investimento.Id));
        }
    }
}

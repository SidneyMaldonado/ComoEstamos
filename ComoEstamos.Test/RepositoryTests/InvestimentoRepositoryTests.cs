using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;

namespace ComoEstamos.Test.RepositoryTests
{
    [TestClass]
    public sealed class InvestimentoRepositoryTests : RepositoryTestBase
    {
        private InvestimentoRepository _repository = null!;
        private Usuario _usuario = null!;
        private Carteira _carteira = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _repository = new InvestimentoRepository(ContextFactory);
            _usuario = await CriarUsuarioAsync();
            _carteira = new Carteira { IdUsuario = _usuario.Id, Nome = "Ações" };
            await SemearAsync(_carteira);
        }

        private Investimento NovoInvestimento(string nome, int? idCarteira = null) => new()
        {
            IdCarteira = idCarteira ?? _carteira.Id,
            Nome = nome,
            Quantidade = 100m,
            Cotacao = 32.5m
        };

        [TestMethod]
        public async Task InserirAsync_PersisteInvestimentoComPrecisaoDecimal()
        {
            var investimento = NovoInvestimento("PETR4");
            investimento.Quantidade = 0.123456m;
            investimento.Cotacao = 350123.654321m;
            investimento.Observacao = "Longo prazo";

            await _repository.InserirAsync(investimento);

            var obtido = await _repository.ObterPorIdAsync(investimento.Id);
            Assert.IsNotNull(obtido);
            Assert.AreEqual("PETR4", obtido.Nome);
            Assert.AreEqual(0.123456m, obtido.Quantidade);
            Assert.AreEqual(350123.654321m, obtido.Cotacao);
            Assert.AreEqual("Longo prazo", obtido.Observacao);
        }

        [TestMethod]
        public async Task ListarPorCarteiraAsync_RetornaSomenteInvestimentosDaCarteiraOrdenadosPorNome()
        {
            var outraCarteira = new Carteira { IdUsuario = _usuario.Id, Nome = "Renda Fixa" };
            await SemearAsync(outraCarteira);
            await SemearAsync(
                NovoInvestimento("VALE3"),
                NovoInvestimento("ITUB4"),
                NovoInvestimento("PETR4"),
                NovoInvestimento("CDB", outraCarteira.Id));

            var lista = await _repository.ListarPorCarteiraAsync(_carteira.Id);

            CollectionAssert.AreEqual(new[] { "ITUB4", "PETR4", "VALE3" }, lista.Select(i => i.Nome).ToArray());
        }

        [TestMethod]
        public async Task ListarPorCarteiraAsync_IgnoraInativosPorPadrao()
        {
            var inativo = NovoInvestimento("OIBR3");
            inativo.Ativo = false;
            await SemearAsync(NovoInvestimento("PETR4"), inativo);

            var lista = await _repository.ListarPorCarteiraAsync(_carteira.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual("PETR4", lista[0].Nome);
        }

        [TestMethod]
        public async Task ListarPorCarteiraAsync_IncluiInativosQuandoSolicitado()
        {
            var inativo = NovoInvestimento("OIBR3");
            inativo.Ativo = false;
            await SemearAsync(NovoInvestimento("PETR4"), inativo);

            var lista = await _repository.ListarPorCarteiraAsync(_carteira.Id, incluirInativos: true);

            Assert.HasCount(2, lista);
        }

        [TestMethod]
        public async Task ListarPorCarteiraAsync_RetornaVazioParaCarteiraSemInvestimentos()
        {
            var lista = await _repository.ListarPorCarteiraAsync(_carteira.Id);

            Assert.IsEmpty(lista);
        }

        [TestMethod]
        public async Task AtualizarAsync_AlteraQuantidadeECotacao()
        {
            var investimento = await _repository.InserirAsync(NovoInvestimento("PETR4"));

            investimento.Quantidade = 200m;
            investimento.Cotacao = 40.1m;
            await _repository.AtualizarAsync(investimento);

            var obtido = await _repository.ObterPorIdAsync(investimento.Id);
            Assert.IsNotNull(obtido);
            Assert.AreEqual(200m, obtido.Quantidade);
            Assert.AreEqual(40.1m, obtido.Cotacao);
        }

        [TestMethod]
        public async Task DesativarAsync_RemoveDaListagemDaCarteira()
        {
            var investimento = await _repository.InserirAsync(NovoInvestimento("PETR4"));

            await _repository.DesativarAsync(investimento.Id);

            Assert.IsEmpty(await _repository.ListarPorCarteiraAsync(_carteira.Id));
        }
    }
}

using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;

namespace ComoEstamos.Test.RepositoryTests
{
    [TestClass]
    public sealed class ParcelaRepositoryTests : RepositoryTestBase
    {
        private ParcelaRepository _repository = null!;
        private Usuario _usuario = null!;
        private Conta _conta = null!;
        private Categoria _categoria = null!;
        private Divida _divida = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _repository = new ParcelaRepository(ContextFactory);
            _usuario = await CriarUsuarioAsync();
            _conta = new Conta { IdUsuario = _usuario.Id, Nome = "Corrente" };
            _categoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Moradia" };
            await SemearAsync(_conta, _categoria);
            _divida = await CriarDividaAsync(_usuario.Id);
        }

        private async Task<Divida> CriarDividaAsync(int idUsuario, bool ativa = true)
        {
            var divida = new Divida
            {
                IdUsuario = idUsuario,
                Nome = "Financiamento",
                DataPrimeiroVencimento = new DateTime(2026, 10, 10),
                Valor = 300m,
                Ativo = ativa
            };
            await SemearAsync(divida);
            return divida;
        }

        private Parcela NovaParcela(DateTime vencimento, int? idDivida = null, string descricao = "Parcela") => new()
        {
            IdDivida = idDivida ?? _divida.Id,
            IdConta = _conta.Id,
            IdCategoria = _categoria.Id,
            Descricao = descricao,
            Valor = 100m,
            DataVencimento = vencimento
        };

        [TestMethod]
        public async Task InserirAsync_PersisteParcela()
        {
            var parcela = await _repository.InserirAsync(NovaParcela(new DateTime(2026, 10, 10), descricao: "1/3"));

            var obtida = await _repository.ObterPorIdAsync(parcela.Id);
            Assert.IsNotNull(obtida);
            Assert.AreEqual("1/3", obtida.Descricao);
            Assert.AreEqual(100m, obtida.Valor);
            Assert.AreEqual(new DateTime(2026, 10, 10), obtida.DataVencimento);
            Assert.IsNull(obtida.DataPagamento);
        }

        [TestMethod]
        public async Task ListarPorDividaAsync_RetornaSomenteParcelasDaDividaOrdenadasPorVencimento()
        {
            var outraDivida = await CriarDividaAsync(_usuario.Id);
            await SemearAsync(
                NovaParcela(new DateTime(2026, 12, 10), descricao: "3/3"),
                NovaParcela(new DateTime(2026, 10, 10), descricao: "1/3"),
                NovaParcela(new DateTime(2026, 11, 10), descricao: "2/3"),
                NovaParcela(new DateTime(2026, 10, 1), outraDivida.Id, "Outra"));

            var lista = await _repository.ListarPorDividaAsync(_divida.Id);

            CollectionAssert.AreEqual(new[] { "1/3", "2/3", "3/3" }, lista.Select(p => p.Descricao).ToArray());
        }

        [TestMethod]
        public async Task ListarPorDividaAsync_IgnoraInativasPorPadrao()
        {
            var inativa = NovaParcela(new DateTime(2026, 11, 10), descricao: "Inativa");
            inativa.Ativo = false;
            await SemearAsync(NovaParcela(new DateTime(2026, 10, 10), descricao: "Ativa"), inativa);

            var lista = await _repository.ListarPorDividaAsync(_divida.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual("Ativa", lista[0].Descricao);
        }

        [TestMethod]
        public async Task ListarPorDividaAsync_IncluiInativasQuandoSolicitado()
        {
            var inativa = NovaParcela(new DateTime(2026, 11, 10));
            inativa.Ativo = false;
            await SemearAsync(NovaParcela(new DateTime(2026, 10, 10)), inativa);

            var lista = await _repository.ListarPorDividaAsync(_divida.Id, incluirInativos: true);

            Assert.HasCount(2, lista);
        }

        [TestMethod]
        public async Task ListarPendentesAsync_RetornaParcelasNaoPagasAteAData()
        {
            var paga = NovaParcela(new DateTime(2026, 9, 10), descricao: "Paga");
            paga.DataPagamento = new DateTime(2026, 9, 9);
            await SemearAsync(
                paga,
                NovaParcela(new DateTime(2026, 10, 10), descricao: "Vence hoje"),
                NovaParcela(new DateTime(2026, 9, 20), descricao: "Atrasada"),
                NovaParcela(new DateTime(2026, 11, 10), descricao: "Futura"));

            var lista = await _repository.ListarPendentesAsync(_usuario.Id, new DateTime(2026, 10, 10));

            CollectionAssert.AreEqual(new[] { "Atrasada", "Vence hoje" }, lista.Select(p => p.Descricao).ToArray());
        }

        [TestMethod]
        public async Task ListarPendentesAsync_CarregaDividaContaECategoria()
        {
            await SemearAsync(NovaParcela(new DateTime(2026, 10, 10)));

            var lista = await _repository.ListarPendentesAsync(_usuario.Id, new DateTime(2026, 10, 31));

            Assert.HasCount(1, lista);
            Assert.AreEqual("Financiamento", lista[0].Divida?.Nome);
            Assert.AreEqual("Corrente", lista[0].Conta?.Nome);
            Assert.AreEqual("Moradia", lista[0].Categoria?.Nome);
        }

        [TestMethod]
        public async Task ListarPendentesAsync_IgnoraParcelasDeOutroUsuario()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var dividaDeOutro = await CriarDividaAsync(outro.Id);
            await SemearAsync(NovaParcela(new DateTime(2026, 10, 10), dividaDeOutro.Id));

            var lista = await _repository.ListarPendentesAsync(_usuario.Id, new DateTime(2026, 10, 31));

            Assert.IsEmpty(lista);
        }

        [TestMethod]
        public async Task ListarPendentesAsync_IgnoraParcelasDeDividaInativa()
        {
            var dividaInativa = await CriarDividaAsync(_usuario.Id, ativa: false);
            await SemearAsync(NovaParcela(new DateTime(2026, 10, 10), dividaInativa.Id));

            var lista = await _repository.ListarPendentesAsync(_usuario.Id, new DateTime(2026, 10, 31));

            Assert.IsEmpty(lista);
        }

        [TestMethod]
        public async Task ListarPendentesAsync_IgnoraParcelasInativas()
        {
            var inativa = NovaParcela(new DateTime(2026, 10, 10));
            inativa.Ativo = false;
            await SemearAsync(inativa);

            var lista = await _repository.ListarPendentesAsync(_usuario.Id, new DateTime(2026, 10, 31));

            Assert.IsEmpty(lista);
        }

        [TestMethod]
        public async Task RegistrarPagamentoAsync_GravaDataDePagamento()
        {
            var parcela = await _repository.InserirAsync(NovaParcela(new DateTime(2026, 10, 10)));
            var dataPagamento = new DateTime(2026, 10, 8);

            await _repository.RegistrarPagamentoAsync(parcela.Id, dataPagamento);

            var obtida = await _repository.ObterPorIdAsync(parcela.Id);
            Assert.IsNotNull(obtida);
            Assert.AreEqual(dataPagamento, obtida.DataPagamento);
            Assert.IsNotNull(obtida.DataAlteracao);
        }

        [TestMethod]
        public async Task RegistrarPagamentoAsync_TiraParcelaDasPendentes()
        {
            var parcela = await _repository.InserirAsync(NovaParcela(new DateTime(2026, 10, 10)));

            await _repository.RegistrarPagamentoAsync(parcela.Id, new DateTime(2026, 10, 8));

            Assert.IsEmpty(await _repository.ListarPendentesAsync(_usuario.Id, new DateTime(2026, 10, 31)));
        }

        [TestMethod]
        public async Task RegistrarPagamentoAsync_LancaExcecaoQuandoParcelaNaoExiste()
        {
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
                () => _repository.RegistrarPagamentoAsync(999, DateTime.Today));
        }
    }
}

using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;

namespace ComoEstamos.Test.RepositoryTests
{
    [TestClass]
    public sealed class DividaRepositoryTests : RepositoryTestBase
    {
        private DividaRepository _repository = null!;

        [TestInitialize]
        public void Inicializar() => _repository = new DividaRepository(ContextFactory);

        private static Divida NovaDivida(int idUsuario, string nome = "Financiamento") => new()
        {
            IdUsuario = idUsuario,
            Nome = nome,
            DataPrimeiroVencimento = new DateTime(2026, 10, 10),
            NumeroParcelas = 12,
            Valor = 1200m
        };

        [TestMethod]
        public async Task InserirAsync_PersisteDividaComTodosOsCampos()
        {
            var usuario = await CriarUsuarioAsync();
            var divida = NovaDivida(usuario.Id);
            divida.DiaVencimento = 15;

            await _repository.InserirAsync(divida);

            var obtida = await _repository.ObterPorIdAsync(divida.Id);
            Assert.IsNotNull(obtida);
            Assert.AreEqual("Financiamento", obtida.Nome);
            Assert.AreEqual(15, obtida.DiaVencimento);
            Assert.AreEqual(new DateTime(2026, 10, 10), obtida.DataPrimeiroVencimento);
            Assert.AreEqual(12, obtida.NumeroParcelas);
            Assert.AreEqual(1200m, obtida.Valor);
            Assert.IsTrue(obtida.EhDivida);
        }

        [TestMethod]
        public void NovaDivida_UsaValoresPadrao()
        {
            var divida = new Divida();

            Assert.AreEqual(10, divida.DiaVencimento);
            Assert.AreEqual(1, divida.NumeroParcelas);
            Assert.IsTrue(divida.EhDivida);
        }

        [TestMethod]
        public async Task InserirAsync_PersisteCredito()
        {
            var usuario = await CriarUsuarioAsync();
            var credito = NovaDivida(usuario.Id, "Empréstimo a amigo");
            credito.EhDivida = false;

            await _repository.InserirAsync(credito);

            var obtido = await _repository.ObterPorIdAsync(credito.Id);
            Assert.IsNotNull(obtido);
            Assert.IsFalse(obtido.EhDivida);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_CarregaCredorContaECategoria()
        {
            var usuario = await CriarUsuarioAsync();
            var credor = new Credor { IdUsuario = usuario.Id, Nome = "Banco X" };
            var conta = new Conta { IdUsuario = usuario.Id, Nome = "Corrente" };
            var categoria = new Categoria { IdUsuario = usuario.Id, Nome = "Moradia" };
            await SemearAsync(credor, conta, categoria);
            var divida = NovaDivida(usuario.Id);
            divida.IdCredor = credor.Id;
            divida.IdConta = conta.Id;
            divida.IdCategoria = categoria.Id;
            await SemearAsync(divida);

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual("Banco X", lista[0].Credor?.Nome);
            Assert.AreEqual("Corrente", lista[0].Conta?.Nome);
            Assert.AreEqual("Moradia", lista[0].Categoria?.Nome);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_AceitaRelacionamentosOpcionaisNulos()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(NovaDivida(usuario.Id));

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            Assert.HasCount(1, lista);
            Assert.IsNull(lista[0].Credor);
            Assert.IsNull(lista[0].Conta);
            Assert.IsNull(lista[0].Categoria);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_RetornaSomenteDividasDoUsuarioOrdenadasPorNome()
        {
            var usuario = await CriarUsuarioAsync("a@teste.com");
            var outro = await CriarUsuarioAsync("b@teste.com");
            await SemearAsync(
                NovaDivida(usuario.Id, "Financiamento"),
                NovaDivida(usuario.Id, "Cartão"),
                NovaDivida(outro.Id, "De outro"));

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            CollectionAssert.AreEqual(new[] { "Cartão", "Financiamento" }, lista.Select(d => d.Nome).ToArray());
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_RetornaDividasECreditos()
        {
            var usuario = await CriarUsuarioAsync();
            var credito = NovaDivida(usuario.Id, "A receber");
            credito.EhDivida = false;
            await SemearAsync(NovaDivida(usuario.Id, "A pagar"), credito);

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            Assert.HasCount(2, lista);
            Assert.AreEqual(1, lista.Count(d => d.EhDivida));
            Assert.AreEqual(1, lista.Count(d => !d.EhDivida));
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_IgnoraInativosPorPadrao()
        {
            var usuario = await CriarUsuarioAsync();
            var inativa = NovaDivida(usuario.Id, "Quitada");
            inativa.Ativo = false;
            await SemearAsync(NovaDivida(usuario.Id, "Em aberto"), inativa);

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual("Em aberto", lista[0].Nome);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_IncluiInativosQuandoSolicitado()
        {
            var usuario = await CriarUsuarioAsync();
            var inativa = NovaDivida(usuario.Id, "Quitada");
            inativa.Ativo = false;
            await SemearAsync(NovaDivida(usuario.Id, "Em aberto"), inativa);

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id, incluirInativos: true);

            Assert.HasCount(2, lista);
        }

        [TestMethod]
        public async Task AtualizarAsync_AlteraValorETipo()
        {
            var usuario = await CriarUsuarioAsync();
            var divida = await _repository.InserirAsync(NovaDivida(usuario.Id));

            divida.Valor = 900m;
            divida.EhDivida = false;
            await _repository.AtualizarAsync(divida);

            var obtida = await _repository.ObterPorIdAsync(divida.Id);
            Assert.IsNotNull(obtida);
            Assert.AreEqual(900m, obtida.Valor);
            Assert.IsFalse(obtida.EhDivida);
        }

        [TestMethod]
        public async Task DesativarAsync_RemoveDaListagemDoUsuario()
        {
            var usuario = await CriarUsuarioAsync();
            var divida = await _repository.InserirAsync(NovaDivida(usuario.Id));

            await _repository.DesativarAsync(divida.Id);

            Assert.IsEmpty(await _repository.ListarPorUsuarioAsync(usuario.Id));
        }
    }
}

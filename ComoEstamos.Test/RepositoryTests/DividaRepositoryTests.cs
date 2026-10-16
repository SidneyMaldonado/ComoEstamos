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

        private static Divida NovaDivida(int? idConta = null, string nome = "Financiamento") => new()
        {
            IdConta = idConta,
            Nome = nome,
            DataPrimeiroVencimento = new DateTime(2026, 10, 10),
            NumeroParcelas = 12,
            Valor = 1200m
        };

        /// <summary>Usuário com uma conta: a dívida não tem id_usuario, o dono é o da conta dela.</summary>
        private async Task<Conta> CriarContaAsync(string email = "usuario@teste.com")
        {
            var usuario = await CriarUsuarioAsync(email);
            var conta = new Conta { IdUsuario = usuario.Id, Nome = "Corrente" };
            await SemearAsync(conta);
            return conta;
        }

        [TestMethod]
        public async Task InserirAsync_PersisteDividaComTodosOsCampos()
        {
            var divida = NovaDivida();
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
            var credito = NovaDivida(nome: "Empréstimo a amigo");
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
            var divida = NovaDivida(conta.Id);
            divida.IdCredor = credor.Id;
            divida.IdCategoria = categoria.Id;
            await SemearAsync(divida);

            var lista = await _repository.ListarPorUsuarioAsync(usuario.Id);

            Assert.HasCount(1, lista);
            Assert.AreEqual("Banco X", lista[0].Credor?.Nome);
            Assert.AreEqual("Corrente", lista[0].Conta?.Nome);
            Assert.AreEqual("Moradia", lista[0].Categoria?.Nome);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_AceitaCredorECategoriaNulos()
        {
            var conta = await CriarContaAsync();
            await SemearAsync(NovaDivida(conta.Id));

            var lista = await _repository.ListarPorUsuarioAsync(conta.IdUsuario);

            Assert.HasCount(1, lista);
            Assert.IsNull(lista[0].Credor);
            Assert.AreEqual("Corrente", lista[0].Conta?.Nome);
            Assert.IsNull(lista[0].Categoria);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_RetornaSomenteDividasDoUsuarioOrdenadasPorNome()
        {
            var conta = await CriarContaAsync("a@teste.com");
            var contaDeOutro = await CriarContaAsync("b@teste.com");
            await SemearAsync(
                NovaDivida(conta.Id, "Financiamento"),
                NovaDivida(conta.Id, "Cartão"),
                NovaDivida(contaDeOutro.Id, "De outro"));

            var lista = await _repository.ListarPorUsuarioAsync(conta.IdUsuario);

            CollectionAssert.AreEqual(new[] { "Cartão", "Financiamento" }, lista.Select(d => d.Nome).ToArray());
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_RetornaDividasECreditos()
        {
            var conta = await CriarContaAsync();
            var credito = NovaDivida(conta.Id, "A receber");
            credito.EhDivida = false;
            await SemearAsync(NovaDivida(conta.Id, "A pagar"), credito);

            var lista = await _repository.ListarPorUsuarioAsync(conta.IdUsuario);

            Assert.HasCount(2, lista);
            Assert.AreEqual(1, lista.Count(d => d.EhDivida));
            Assert.AreEqual(1, lista.Count(d => !d.EhDivida));
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_IgnoraInativosPorPadrao()
        {
            var conta = await CriarContaAsync();
            var inativa = NovaDivida(conta.Id, "Quitada");
            inativa.Ativo = false;
            await SemearAsync(NovaDivida(conta.Id, "Em aberto"), inativa);

            var lista = await _repository.ListarPorUsuarioAsync(conta.IdUsuario);

            Assert.HasCount(1, lista);
            Assert.AreEqual("Em aberto", lista[0].Nome);
        }

        [TestMethod]
        public async Task ListarPorUsuarioAsync_IncluiInativosQuandoSolicitado()
        {
            var conta = await CriarContaAsync();
            var inativa = NovaDivida(conta.Id, "Quitada");
            inativa.Ativo = false;
            await SemearAsync(NovaDivida(conta.Id, "Em aberto"), inativa);

            var lista = await _repository.ListarPorUsuarioAsync(conta.IdUsuario, incluirInativos: true);

            Assert.HasCount(2, lista);
        }

        [TestMethod]
        public async Task AtualizarAsync_AlteraValorETipo()
        {
            var divida = await _repository.InserirAsync(NovaDivida());

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
            var conta = await CriarContaAsync();
            var divida = await _repository.InserirAsync(NovaDivida(conta.Id));

            await _repository.DesativarAsync(divida.Id);

            Assert.IsEmpty(await _repository.ListarPorUsuarioAsync(conta.IdUsuario));
        }

        [TestMethod]
        public async Task ExisteAtivaPorContaCategoriaECredor_ConsideramSoDividasAtivas()
        {
            var usuario = await CriarUsuarioAsync();
            var conta = new Conta { IdUsuario = usuario.Id, Nome = "Corrente" };
            var outraConta = new Conta { IdUsuario = usuario.Id, Nome = "Poupança" };
            var categoria = new Categoria { IdUsuario = usuario.Id, Nome = "Moradia" };
            var credor = new Credor { IdUsuario = usuario.Id, Nome = "Banco" };
            await SemearAsync(conta, outraConta, categoria, credor);
            var ativa = NovaDivida(conta.Id);
            ativa.IdCategoria = categoria.Id;
            ativa.IdCredor = credor.Id;
            var inativa = NovaDivida(outraConta.Id);
            inativa.Ativo = false;
            await SemearAsync(ativa, inativa);

            Assert.IsTrue(await _repository.ExisteAtivaPorContaAsync(conta.Id));
            Assert.IsTrue(await _repository.ExisteAtivaPorCategoriaAsync(categoria.Id));
            Assert.IsTrue(await _repository.ExisteAtivaPorCredorAsync(credor.Id));
            Assert.IsFalse(await _repository.ExisteAtivaPorContaAsync(outraConta.Id));
        }

        [TestMethod]
        public async Task ExisteAtivaPorCredorAsync_RetornaFalsoSemDividas()
        {
            Assert.IsFalse(await _repository.ExisteAtivaPorCredorAsync(1));
        }
    }
}

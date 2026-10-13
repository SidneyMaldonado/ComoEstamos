using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;

namespace ComoEstamos.Test.UseCaseTests
{
    [TestClass]
    public sealed class BuscarDividaPorIdUseCaseTests : RepositoryTestBase
    {
        private BuscarDividaPorIdUseCase _useCase = null!;
        private Usuario _usuario = null!;
        private Conta _conta = null!;
        private Categoria _categoria = null!;
        private Credor _credor = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _useCase = new BuscarDividaPorIdUseCase(new UnitOfWorkFactory(ContextFactory));
            _usuario = await CriarUsuarioAsync();
            _conta = new Conta { IdUsuario = _usuario.Id, Nome = "Corrente" };
            _categoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Compras" };
            _credor = new Credor { IdUsuario = _usuario.Id, Nome = "Loja" };
            await SemearAsync(_conta, _categoria, _credor);
        }

        private async Task<Divida> CriarDividaAsync(bool ativa = true, int? idUsuario = null)
        {
            var divida = new Divida
            {
                IdUsuario = idUsuario ?? _usuario.Id,
                IdConta = _conta.Id,
                IdCategoria = _categoria.Id,
                IdCredor = _credor.Id,
                Nome = "Notebook",
                NumeroParcelas = 2,
                Valor = 300m,
                DataPrimeiroVencimento = new DateTime(2026, 10, 10),
                Ativo = ativa
            };
            var inativa = NovaParcela(divida, "Inativa", new DateTime(2026, 9, 10));
            inativa.Ativo = false;
            divida.Parcelas =
            [
                NovaParcela(divida, "2/2", new DateTime(2026, 11, 10)),
                NovaParcela(divida, "1/2", new DateTime(2026, 10, 10)),
                inativa
            ];
            await SemearAsync(divida);
            return divida;
        }

        private Parcela NovaParcela(Divida divida, string descricao, DateTime vencimento) => new()
        {
            Divida = divida,
            IdConta = _conta.Id,
            IdCategoria = _categoria.Id,
            Descricao = descricao,
            Valor = 150m,
            DataVencimento = vencimento
        };

        [TestMethod]
        public async Task ExecutarAsync_RetornaDividaComCadastrosEParcelasAtivasPorVencimento()
        {
            var divida = await CriarDividaAsync();

            var obtida = await _useCase.ExecutarAsync(divida.Id, _usuario.Id);

            Assert.IsNotNull(obtida);
            Assert.AreEqual("Notebook", obtida.Nome);
            Assert.AreEqual("Corrente", obtida.Conta?.Nome);
            Assert.AreEqual("Compras", obtida.Categoria?.Nome);
            Assert.AreEqual("Loja", obtida.Credor?.Nome);
            CollectionAssert.AreEqual(new[] { "1/2", "2/2" }, obtida.Parcelas.Select(p => p.Descricao).ToArray());
        }

        [TestMethod]
        public async Task ExecutarAsync_SemCredorDeixaCredorNulo()
        {
            var divida = await CriarDividaAsync();
            await using (var db = CriarContexto())
            {
                db.Dividas.Single().IdCredor = null;
                await db.SaveChangesAsync();
            }

            var obtida = await _useCase.ExecutarAsync(divida.Id, _usuario.Id);

            Assert.IsNotNull(obtida);
            Assert.IsNull(obtida.Credor);
        }

        [TestMethod]
        public async Task ExecutarAsync_RetornaNuloParaInexistenteInativaOuDeOutroUsuario()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var inativa = await CriarDividaAsync(ativa: false);
            var deOutro = await CriarDividaAsync(idUsuario: outro.Id);

            Assert.IsNull(await _useCase.ExecutarAsync(999, _usuario.Id));
            Assert.IsNull(await _useCase.ExecutarAsync(inativa.Id, _usuario.Id));
            Assert.IsNull(await _useCase.ExecutarAsync(deOutro.Id, _usuario.Id));
        }
    }
}

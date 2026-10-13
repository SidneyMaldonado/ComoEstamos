using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.ServiceTests
{
    /// <summary>O service repassa cada operação ao caso de uso com o usuário atual.</summary>
    [TestClass]
    public sealed class DividaServiceTests : RepositoryTestBase
    {
        private DividaService _service = null!;
        private UsuarioAtual _usuarioAtual = null!;

        [TestInitialize]
        public void Inicializar()
        {
            var uow = new UnitOfWorkFactory(ContextFactory);
            _usuarioAtual = new UsuarioAtual(new UsuarioRepository(ContextFactory));
            _service = new DividaService(
                _usuarioAtual,
                new BuscarDividasUseCase(uow),
                new BuscarDividaPorIdUseCase(uow),
                new CriarDividaUseCase(uow),
                new AlterarDividaUseCase(uow),
                new ExcluirDividaUseCase(uow));
        }

        /// <summary>Dívida válida, com conta e categoria do usuário atual.</summary>
        private async Task<Divida> NovaDividaAsync()
        {
            var idUsuario = await _usuarioAtual.ObterIdAsync();
            var conta = new Conta { IdUsuario = idUsuario, Nome = "Corrente" };
            var categoria = new Categoria { IdUsuario = idUsuario, Nome = "Compras" };
            await SemearAsync(conta, categoria);
            return new Divida
            {
                IdConta = conta.Id,
                IdCategoria = categoria.Id,
                Nome = "Notebook",
                Valor = 300m,
                NumeroParcelas = 3,
                DiaVencimento = 10,
                DataPrimeiroVencimento = new DateTime(2026, 10, 10)
            };
        }

        [TestMethod]
        public async Task IncluirAsync_GravaParaOUsuarioAtual()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var divida = await NovaDividaAsync();
            divida.IdUsuario = outro.Id;

            await _service.IncluirAsync(divida);

            await using var db = CriarContexto();
            Assert.AreEqual(await _usuarioAtual.ObterIdAsync(), (await db.Dividas.SingleAsync()).IdUsuario);
        }

        [TestMethod]
        public async Task BuscarEBuscarPorId_UsamOUsuarioAtual()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            var deOutro = new Divida { IdUsuario = outro.Id, Nome = "De outro", DataPrimeiroVencimento = new DateTime(2026, 10, 10) };
            await SemearAsync(deOutro);
            var divida = await _service.IncluirAsync(await NovaDividaAsync());

            var lista = await _service.BuscarAsync(new FiltroDividas());

            Assert.AreEqual("Notebook", lista.Single().Nome);
            Assert.HasCount(3, (await _service.BuscarPorIdAsync(divida.Id))!.Parcelas);
            Assert.IsNull(await _service.BuscarPorIdAsync(deOutro.Id));
        }

        [TestMethod]
        public async Task AlterarEExcluir_UsamOUsuarioAtual()
        {
            var divida = await _service.IncluirAsync(await NovaDividaAsync());
            divida.Nome = "Notebook Dell";

            await _service.AlterarAsync(divida);
            Assert.AreEqual("Notebook Dell", (await _service.BuscarPorIdAsync(divida.Id))?.Nome);

            await _service.ExcluirAsync(divida.Id);
            Assert.IsNull(await _service.BuscarPorIdAsync(divida.Id));
        }
    }
}

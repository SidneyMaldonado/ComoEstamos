using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Core.Validacao;
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
                new BuscarResumoMesUseCase(uow),
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
        public async Task IncluirAsync_RecusaContaDeOutroUsuario()
        {
            var divida = await NovaDividaAsync();
            divida.IdConta = (await CriarContaDeOutroUsuarioAsync()).Id;

            await Assert.ThrowsExactlyAsync<ValidacaoException>(() => _service.IncluirAsync(divida));

            await using var db = CriarContexto();
            Assert.IsFalse(await db.Dividas.AnyAsync());
        }

        [TestMethod]
        public async Task BuscarEBuscarPorId_UsamOUsuarioAtual()
        {
            var contaDeOutro = await CriarContaDeOutroUsuarioAsync();
            var deOutro = new Divida { IdConta = contaDeOutro.Id, Nome = "De outro", DataPrimeiroVencimento = new DateTime(2026, 10, 10) };
            await SemearAsync(deOutro);
            var divida = await _service.IncluirAsync(await NovaDividaAsync());

            var lista = await _service.BuscarAsync(new FiltroDividas());

            Assert.AreEqual("Notebook", lista.Single().Nome);
            Assert.HasCount(3, (await _service.BuscarPorIdAsync(divida.Id))!.Parcelas);
            Assert.IsNull(await _service.BuscarPorIdAsync(deOutro.Id));
        }

        [TestMethod]
        public async Task BuscarResumoMes_UsaOUsuarioAtual()
        {
            var contaDeOutro = await CriarContaDeOutroUsuarioAsync();
            var deOutro = new Divida { IdConta = contaDeOutro.Id, Nome = "De outro", DataPrimeiroVencimento = new DateTime(2026, 10, 10) };
            deOutro.Parcelas.Add(new Parcela { Divida = deOutro, Descricao = "De outro 1/1", Valor = 50m, DataVencimento = new DateTime(2026, 10, 10) });
            await SemearAsync(deOutro);
            await _service.IncluirAsync(await NovaDividaAsync());

            var resumo = await _service.BuscarResumoMesAsync(new DateTime(2026, 10, 1));

            Assert.AreEqual("Notebook", resumo.Despesas.Single().Nome);
            Assert.AreEqual(100m, resumo.TotalDespesas);
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

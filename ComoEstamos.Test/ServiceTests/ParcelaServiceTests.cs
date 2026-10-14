using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Core.UseCases.Parcelas;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.ServiceTests
{
    /// <summary>O service repassa cada operação ao caso de uso com o usuário atual.</summary>
    [TestClass]
    public sealed class ParcelaServiceTests : RepositoryTestBase
    {
        private ParcelaService _service = null!;
        private UsuarioAtual _usuarioAtual = null!;
        private UnitOfWorkFactory _uow = null!;

        [TestInitialize]
        public void Inicializar()
        {
            _uow = new UnitOfWorkFactory(ContextFactory);
            _usuarioAtual = new UsuarioAtual(new UsuarioRepository(ContextFactory));
            _service = new ParcelaService(
                _usuarioAtual,
                new BuscarParcelaPorIdUseCase(_uow),
                new IncluirParcelaUseCase(_uow),
                new AlterarParcelaUseCase(_uow),
                new ExcluirParcelaUseCase(_uow),
                new PagarParcelaUseCase(_uow));
        }

        /// <summary>Dívida de R$ 300 em 3x do usuário atual; devolve as parcelas por vencimento.</summary>
        private async Task<List<Parcela>> CriarParcelasAsync()
        {
            var idUsuario = await _usuarioAtual.ObterIdAsync();
            var conta = new Conta { IdUsuario = idUsuario, Nome = "Corrente", Saldo = 1000m };
            var categoria = new Categoria { IdUsuario = idUsuario, Nome = "Compras" };
            await SemearAsync(conta, categoria);
            await new CriarDividaUseCase(_uow).ExecutarAsync(new Divida
            {
                IdConta = conta.Id,
                IdCategoria = categoria.Id,
                Nome = "Notebook",
                Valor = 300m,
                NumeroParcelas = 3,
                DiaVencimento = 10,
                DataPrimeiroVencimento = new DateTime(2026, 10, 10)
            }, idUsuario);

            await using var db = CriarContexto();
            return await db.Parcelas.OrderBy(p => p.DataVencimento).ToListAsync();
        }

        [TestMethod]
        public async Task BuscarPorIdEAlterar_UsamOUsuarioAtual()
        {
            var parcela = (await CriarParcelasAsync())[0];
            parcela.Valor = 150m;

            await _service.AlterarAsync(parcela);

            Assert.AreEqual(150m, (await _service.BuscarPorIdAsync(parcela.Id))?.Valor);
        }

        [TestMethod]
        public async Task PagarEExcluir_UsamOUsuarioAtual()
        {
            var parcelas = await CriarParcelasAsync();

            await _service.PagarAsync(parcelas[0].Id, new DateTime(2026, 10, 8));
            await _service.ExcluirAsync(parcelas[2].Id);

            Assert.AreEqual(new DateTime(2026, 10, 8), (await _service.BuscarPorIdAsync(parcelas[0].Id))?.DataPagamento);
            Assert.IsNull(await _service.BuscarPorIdAsync(parcelas[2].Id));
            await using var db = CriarContexto();
            Assert.AreEqual(900m, (await db.Contas.SingleAsync()).Saldo);
        }
    }
}

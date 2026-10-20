using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases.Operacoes;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.ServiceTests
{
    /// <summary>O service repassa cada operação ao caso de uso com o usuário atual (dono das carteiras).</summary>
    [TestClass]
    public sealed class OperacaoServiceTests : RepositoryTestBase
    {
        private OperacaoService _service = null!;
        private Investimento _investimento = null!;
        private Investimento _alheio = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            var uow = new UnitOfWorkFactory(ContextFactory);
            var usuarioAtual = new UsuarioAtual(new UsuarioRepository(ContextFactory));
            _service = new OperacaoService(
                usuarioAtual,
                new BuscarUltimasOperacoesUseCase(uow),
                new RegistrarOperacaoUseCase(uow),
                new ExcluirOperacaoUseCase(uow));

            var outro = await CriarUsuarioAsync("outro@teste.com");
            var carteira = new Carteira { IdUsuario = await usuarioAtual.ObterIdAsync(), Nome = "Ações" };
            var alheia = new Carteira { IdUsuario = outro.Id, Nome = "Alheia" };
            await SemearAsync(carteira, alheia);
            _investimento = new Investimento { IdCarteira = carteira.Id, Nome = "PETR4", Quantidade = 10m };
            _alheio = new Investimento { IdCarteira = alheia.Id, Nome = "VALE3", Quantidade = 10m };
            await SemearAsync(_investimento, _alheio);
        }

        private static Operacao Compra(int idInvestimento) => new()
        {
            IdInvestimento = idInvestimento,
            EhCompra = true,
            Quantidade = 5m,
            ValorTotal = 160m,
            DataOperacao = new DateTime(2026, 9, 30, 14, 35, 0)
        };

        [TestMethod]
        public async Task RegistrarBuscarEExcluir_UsamOUsuarioAtual()
        {
            var operacao = await _service.RegistrarAsync(Compra(_investimento.Id));

            Assert.AreEqual(operacao.Id, (await _service.BuscarUltimasAsync(_investimento.Id, 10)).Single().Id);

            await _service.ExcluirAsync(operacao.Id);

            Assert.IsEmpty(await _service.BuscarUltimasAsync(_investimento.Id, 10));
            await using var db = CriarContexto();
            Assert.AreEqual(10m, (await db.Investimentos.SingleAsync(i => i.Id == _investimento.Id)).Quantidade);
        }

        [TestMethod]
        public async Task RegistrarAsync_RecusaInvestimentoDeOutroUsuario()
        {
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _service.RegistrarAsync(Compra(_alheio.Id)));
        }
    }
}

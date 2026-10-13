using ComoEstamos.Core.Services;
using ComoEstamos.Core.UseCases.Investimentos;
using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;
using ComoEstamos.Test.RepositoryTests;

namespace ComoEstamos.Test.ServiceTests
{
    /// <summary>O service repassa cada operação ao caso de uso com o usuário atual (dono das carteiras).</summary>
    [TestClass]
    public sealed class InvestimentoServiceTests : RepositoryTestBase
    {
        private InvestimentoService _service = null!;
        private Carteira _carteira = null!;
        private Carteira _alheia = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            var uow = new UnitOfWorkFactory(ContextFactory);
            var usuarioAtual = new UsuarioAtual(new UsuarioRepository(ContextFactory));
            _service = new InvestimentoService(
                usuarioAtual,
                new BuscarTodosInvestimentosUseCase(uow),
                new BuscarInvestimentoPorIdUseCase(uow),
                new IncluirInvestimentoUseCase(uow),
                new AlterarInvestimentoUseCase(uow),
                new ExcluirInvestimentoUseCase(uow));

            var outro = await CriarUsuarioAsync("outro@teste.com");
            _carteira = new Carteira { IdUsuario = await usuarioAtual.ObterIdAsync(), Nome = "Ações" };
            _alheia = new Carteira { IdUsuario = outro.Id, Nome = "Alheia" };
            await SemearAsync(_carteira, _alheia);
        }

        [TestMethod]
        public async Task IncluirAsync_AceitaSoCarteiraDoUsuarioAtual()
        {
            await _service.IncluirAsync(new Investimento { IdCarteira = _carteira.Id, Nome = "PETR4" });

            await Assert.ThrowsExactlyAsync<ValidacaoException>(
                () => _service.IncluirAsync(new Investimento { IdCarteira = _alheia.Id, Nome = "VALE3" }));
        }

        [TestMethod]
        public async Task BuscarTodosAsync_RetornaSoOsDasCarteirasDoUsuarioAtual()
        {
            await SemearAsync(new Investimento { IdCarteira = _alheia.Id, Nome = "BBAS3" });
            await _service.IncluirAsync(new Investimento { IdCarteira = _carteira.Id, Nome = "PETR4" });

            var lista = await _service.BuscarTodosAsync();

            Assert.AreEqual("PETR4", lista.Single().Nome);
        }

        [TestMethod]
        public async Task BuscarAlterarEExcluir_UsamOUsuarioAtual()
        {
            var investimento = await _service.IncluirAsync(new Investimento { IdCarteira = _carteira.Id, Nome = "PETR4" });

            await _service.AlterarAsync(new Investimento { Id = investimento.Id, IdCarteira = _carteira.Id, Nome = "VALE3" });
            Assert.AreEqual("VALE3", (await _service.BuscarPorIdAsync(investimento.Id))?.Nome);

            await _service.ExcluirAsync(investimento.Id);
            Assert.IsNull(await _service.BuscarPorIdAsync(investimento.Id));
        }
    }
}

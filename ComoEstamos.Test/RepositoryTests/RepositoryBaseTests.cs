using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.RepositoryTests
{
    [TestClass]
    public sealed class RepositoryBaseTests : RepositoryTestBase
    {
        /// <summary>Subclasse mínima para exercitar o RepositoryBase (abstrato) contra o banco real.</summary>
        private sealed class CarteiraBaseRepository(IDbContextFactory<AppDbContext> contextFactory)
            : RepositoryBase<Carteira>(contextFactory);

        private CarteiraBaseRepository _repository = null!;

        [TestInitialize]
        public void Inicializar() => _repository = new CarteiraBaseRepository(ContextFactory);

        [TestMethod]
        public async Task InserirAsync_GeraIdEPreencheDataCriacao()
        {
            var usuario = await CriarUsuarioAsync();
            var antes = DateTime.Now;

            var carteira = await _repository.InserirAsync(new Carteira { IdUsuario = usuario.Id, Nome = "Ações" });

            Assert.IsGreaterThan(0, carteira.Id);
            Assert.IsTrue(carteira.Ativo);
            Assert.IsGreaterThanOrEqualTo(antes, carteira.DataCriacao);
            Assert.IsNull(carteira.DataAlteracao);
        }

        [TestMethod]
        public async Task InserirAsync_NaoInsereNavegacoesPreenchidas()
        {
            var usuario = await CriarUsuarioAsync();
            var carteira = new Carteira
            {
                IdUsuario = usuario.Id,
                Nome = "Ações",
                Investimentos = [new Investimento { Nome = "PETR4" }]
            };

            await _repository.InserirAsync(carteira);

            await using var db = CriarContexto();
            Assert.AreEqual(0, await db.Investimentos.CountAsync());
        }

        [TestMethod]
        public async Task ObterPorIdAsync_RetornaEntidadeExistente()
        {
            var usuario = await CriarUsuarioAsync();
            var carteira = new Carteira { IdUsuario = usuario.Id, Nome = "Ações" };
            await SemearAsync(carteira);

            var obtida = await _repository.ObterPorIdAsync(carteira.Id);

            Assert.IsNotNull(obtida);
            Assert.AreEqual("Ações", obtida.Nome);
            Assert.AreEqual(usuario.Id, obtida.IdUsuario);
        }

        [TestMethod]
        public async Task ObterPorIdAsync_RetornaNuloQuandoNaoExiste()
        {
            var obtida = await _repository.ObterPorIdAsync(999);

            Assert.IsNull(obtida);
        }

        [TestMethod]
        public async Task ObterPorIdAsync_RetornaInclusiveInativos()
        {
            var usuario = await CriarUsuarioAsync();
            var carteira = new Carteira { IdUsuario = usuario.Id, Nome = "Antiga", Ativo = false };
            await SemearAsync(carteira);

            var obtida = await _repository.ObterPorIdAsync(carteira.Id);

            Assert.IsNotNull(obtida);
            Assert.IsFalse(obtida.Ativo);
        }

        [TestMethod]
        public async Task ListarAsync_IgnoraInativosPorPadrao()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(
                new Carteira { IdUsuario = usuario.Id, Nome = "Ativa" },
                new Carteira { IdUsuario = usuario.Id, Nome = "Inativa", Ativo = false });

            var lista = await _repository.ListarAsync();

            Assert.HasCount(1, lista);
            Assert.AreEqual("Ativa", lista[0].Nome);
        }

        [TestMethod]
        public async Task ListarAsync_IncluiInativosQuandoSolicitado()
        {
            var usuario = await CriarUsuarioAsync();
            await SemearAsync(
                new Carteira { IdUsuario = usuario.Id, Nome = "Ativa" },
                new Carteira { IdUsuario = usuario.Id, Nome = "Inativa", Ativo = false });

            var lista = await _repository.ListarAsync(incluirInativos: true);

            Assert.HasCount(2, lista);
        }

        [TestMethod]
        public async Task AtualizarAsync_GravaAlteracoesEPreencheDataAlteracao()
        {
            var usuario = await CriarUsuarioAsync();
            var carteira = await _repository.InserirAsync(new Carteira { IdUsuario = usuario.Id, Nome = "Ações" });
            var dataCriacao = carteira.DataCriacao;

            carteira.Nome = "Renda Variável";
            await _repository.AtualizarAsync(carteira);

            var obtida = await _repository.ObterPorIdAsync(carteira.Id);
            Assert.IsNotNull(obtida);
            Assert.AreEqual("Renda Variável", obtida.Nome);
            Assert.IsNotNull(obtida.DataAlteracao);
            Assert.AreEqual(dataCriacao, obtida.DataCriacao);
        }

        [TestMethod]
        public async Task AtualizarAsync_NaoSobrescreveDataCriacao()
        {
            var usuario = await CriarUsuarioAsync();
            var carteira = await _repository.InserirAsync(new Carteira { IdUsuario = usuario.Id, Nome = "Ações" });
            var dataCriacao = carteira.DataCriacao;

            // Simula uma entidade vinda da tela com a data de criação zerada.
            await _repository.AtualizarAsync(new Carteira { Id = carteira.Id, IdUsuario = usuario.Id, Nome = "Ações" });

            var obtida = await _repository.ObterPorIdAsync(carteira.Id);
            Assert.IsNotNull(obtida);
            Assert.AreEqual(dataCriacao, obtida.DataCriacao);
        }

        [TestMethod]
        public async Task DesativarAsync_MarcaComoInativoSemApagar()
        {
            var usuario = await CriarUsuarioAsync();
            var carteira = await _repository.InserirAsync(new Carteira { IdUsuario = usuario.Id, Nome = "Ações" });

            await _repository.DesativarAsync(carteira.Id);

            var obtida = await _repository.ObterPorIdAsync(carteira.Id);
            Assert.IsNotNull(obtida);
            Assert.IsFalse(obtida.Ativo);
            Assert.IsNotNull(obtida.DataAlteracao);
            Assert.IsEmpty(await _repository.ListarAsync());
        }

        [TestMethod]
        public async Task ReativarAsync_MarcaComoAtivo()
        {
            var usuario = await CriarUsuarioAsync();
            var carteira = new Carteira { IdUsuario = usuario.Id, Nome = "Ações", Ativo = false };
            await SemearAsync(carteira);

            await _repository.ReativarAsync(carteira.Id);

            var obtida = await _repository.ObterPorIdAsync(carteira.Id);
            Assert.IsNotNull(obtida);
            Assert.IsTrue(obtida.Ativo);
        }

        [TestMethod]
        public async Task DesativarAsync_LancaExcecaoQuandoNaoExiste()
        {
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _repository.DesativarAsync(999));
        }

        [TestMethod]
        public async Task ReativarAsync_LancaExcecaoQuandoNaoExiste()
        {
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _repository.ReativarAsync(999));
        }
    }
}

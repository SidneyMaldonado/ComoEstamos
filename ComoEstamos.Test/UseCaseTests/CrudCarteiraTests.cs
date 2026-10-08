using ComoEstamos.Core;
using ComoEstamos.Core.UseCases.Carteiras;
using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    /// <summary>Os 5 casos de uso do CRUD de carteira (Spec/07-Crud.md).</summary>
    [TestClass]
    public sealed class CrudCarteiraTests : RepositoryTestBase
    {
        private UnitOfWorkFactory _uow = null!;
        private Usuario _usuario = null!;
        private Usuario _outroUsuario = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _uow = new UnitOfWorkFactory(ContextFactory);
            _usuario = await CriarUsuarioAsync();
            _outroUsuario = await CriarUsuarioAsync("outro@teste.com");
        }

        private async Task<Carteira> SemearCarteiraAsync(string nome, int? idUsuario = null, bool ativa = true)
        {
            var carteira = new Carteira { IdUsuario = idUsuario ?? _usuario.Id, Nome = nome, Ativo = ativa };
            await SemearAsync(carteira);
            return carteira;
        }

        private async Task<List<Carteira>> GravadasAsync()
        {
            await using var db = CriarContexto();
            return await db.Carteiras.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
        }

        private static async Task<ValidacaoException> RecusaCampoAsync(string campo, Func<Task> acao)
        {
            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(acao);
            Assert.IsTrue(ex.Erros.Any(e => e.Campo == campo), $"Esperava erro em {campo}: {ex.Message}");
            return ex;
        }

        // ---------- Incluir ----------

        [TestMethod]
        public async Task Incluir_GravaAtivaDoUsuarioRecebidoSemEspacos()
        {
            var carteira = await new IncluirCarteiraUseCase(_uow).ExecutarAsync(
                new Carteira { Id = 99, IdUsuario = _outroUsuario.Id, Nome = "  Ações ", Ativo = false },
                _usuario.Id);

            var gravada = (await GravadasAsync()).Single();
            Assert.AreEqual(carteira.Id, gravada.Id);
            Assert.AreEqual(_usuario.Id, gravada.IdUsuario);
            Assert.AreEqual("Ações", gravada.Nome);
            Assert.IsTrue(gravada.Ativo);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public async Task Incluir_RecusaNomeVazioSemGravar(string nome)
        {
            await RecusaCampoAsync(nameof(Carteira.Nome),
                () => new IncluirCarteiraUseCase(_uow).ExecutarAsync(new Carteira { Nome = nome }, _usuario.Id));

            Assert.IsEmpty(await GravadasAsync());
        }

        [TestMethod]
        public async Task Incluir_RecusaNomeAcimaDe50CaracteresEAceita50()
        {
            await RecusaCampoAsync(nameof(Carteira.Nome),
                () => new IncluirCarteiraUseCase(_uow).ExecutarAsync(new Carteira { Nome = new string('x', 51) }, _usuario.Id));
            await new IncluirCarteiraUseCase(_uow).ExecutarAsync(new Carteira { Nome = new string('x', 50) }, _usuario.Id);

            Assert.HasCount(1, await GravadasAsync());
        }

        [TestMethod]
        public async Task Incluir_RecusaNomeRepetidoSemDiferenciarMaiusculas()
        {
            await SemearCarteiraAsync("Ações");

            var ex = await RecusaCampoAsync(nameof(Carteira.Nome),
                () => new IncluirCarteiraUseCase(_uow).ExecutarAsync(new Carteira { Nome = " AÇÕES " }, _usuario.Id));

            Assert.AreEqual("Já existe uma carteira com este nome.", ex.Erros.Single().Mensagem);
            Assert.HasCount(1, await GravadasAsync());
        }

        [TestMethod]
        public async Task Incluir_AceitaNomeDeInativaOuDeOutroUsuario()
        {
            await SemearCarteiraAsync("Ações", ativa: false);
            await SemearCarteiraAsync("Renda Fixa", idUsuario: _outroUsuario.Id);

            await new IncluirCarteiraUseCase(_uow).ExecutarAsync(new Carteira { Nome = "Ações" }, _usuario.Id);
            await new IncluirCarteiraUseCase(_uow).ExecutarAsync(new Carteira { Nome = "Renda Fixa" }, _usuario.Id);

            Assert.HasCount(4, await GravadasAsync());
        }

        // ---------- Alterar ----------

        [TestMethod]
        public async Task Alterar_GravaOsCamposEditaveis()
        {
            var original = await SemearCarteiraAsync("Ações");

            await new AlterarCarteiraUseCase(_uow).ExecutarAsync(new Carteira
            {
                Id = original.Id,
                IdUsuario = _outroUsuario.Id,
                Nome = " Renda Fixa ",
            }, _usuario.Id);

            var gravada = (await GravadasAsync()).Single();
            Assert.AreEqual("Renda Fixa", gravada.Nome);
            Assert.AreEqual(_usuario.Id, gravada.IdUsuario);
            Assert.IsNotNull(gravada.DataAlteracao);
        }

        [TestMethod]
        public async Task Alterar_AceitaManterOProprioNome()
        {
            var carteira = await SemearCarteiraAsync("Ações");

            await new AlterarCarteiraUseCase(_uow).ExecutarAsync(new Carteira { Id = carteira.Id, Nome = "AÇÕES" }, _usuario.Id);

            Assert.AreEqual("AÇÕES", (await GravadasAsync()).Single().Nome);
        }

        [TestMethod]
        public async Task Alterar_RecusaNomeDeOutraSemGravar()
        {
            await SemearCarteiraAsync("Renda Fixa");
            var carteira = await SemearCarteiraAsync("Ações");

            await RecusaCampoAsync(nameof(Carteira.Nome),
                () => new AlterarCarteiraUseCase(_uow).ExecutarAsync(new Carteira { Id = carteira.Id, Nome = "renda fixa" }, _usuario.Id));

            Assert.AreEqual("Ações", (await GravadasAsync()).Last().Nome);
        }

        [TestMethod]
        public async Task Alterar_InexistenteOuDeOutroUsuarioNaoEncontra()
        {
            var alheio = await SemearCarteiraAsync("Alheio", idUsuario: _outroUsuario.Id);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
                () => new AlterarCarteiraUseCase(_uow).ExecutarAsync(new Carteira { Id = 999, Nome = "X" }, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
                () => new AlterarCarteiraUseCase(_uow).ExecutarAsync(new Carteira { Id = alheio.Id, Nome = "X" }, _usuario.Id));

            Assert.AreEqual("Alheio", (await GravadasAsync()).Single().Nome);
        }

        [TestMethod]
        public async Task Alterar_RecusaInativa()
        {
            var carteira = await SemearCarteiraAsync("Ações", ativa: false);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => new AlterarCarteiraUseCase(_uow).ExecutarAsync(new Carteira { Id = carteira.Id, Nome = "X" }, _usuario.Id));
        }

        // ---------- Excluir ----------

        [TestMethod]
        public async Task Excluir_Desativa()
        {
            var carteira = await SemearCarteiraAsync("Ações");

            await new ExcluirCarteiraUseCase(_uow).ExecutarAsync(carteira.Id, _usuario.Id);

            Assert.IsFalse((await GravadasAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_RecusaComInvestimentoAtivoSemDesativar()
        {
            var carteira = await SemearCarteiraAsync("Ações");
            await SemearAsync(new Investimento { IdCarteira = carteira.Id, Nome = "PETR4", Ativo = true });

            var ex = await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => new ExcluirCarteiraUseCase(_uow).ExecutarAsync(carteira.Id, _usuario.Id));

            Assert.AreEqual("Não é possível excluir: existem investimentos ativos nesta carteira.", ex.Message);
            Assert.IsTrue((await GravadasAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_DependenteInativoNaoImpede()
        {
            var carteira = await SemearCarteiraAsync("Ações");
            await SemearAsync(new Investimento { IdCarteira = carteira.Id, Nome = "PETR4", Ativo = false });

            await new ExcluirCarteiraUseCase(_uow).ExecutarAsync(carteira.Id, _usuario.Id);

            Assert.IsFalse((await GravadasAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_InexistenteOuDeOutroUsuarioNaoEncontra()
        {
            var alheio = await SemearCarteiraAsync("Alheio", idUsuario: _outroUsuario.Id);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => new ExcluirCarteiraUseCase(_uow).ExecutarAsync(999, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => new ExcluirCarteiraUseCase(_uow).ExecutarAsync(alheio.Id, _usuario.Id));

            Assert.IsTrue((await GravadasAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_RecusaJaExcluida()
        {
            var carteira = await SemearCarteiraAsync("Ações", ativa: false);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => new ExcluirCarteiraUseCase(_uow).ExecutarAsync(carteira.Id, _usuario.Id));
        }

        // ---------- Buscar ----------

        [TestMethod]
        public async Task BuscarTodas_RetornaSoAtivasDoUsuarioPorNome()
        {
            await SemearCarteiraAsync("Renda Fixa");
            await SemearCarteiraAsync("Ações");
            await SemearCarteiraAsync("Antiga", ativa: false);
            await SemearCarteiraAsync("Alheio", idUsuario: _outroUsuario.Id);

            var lista = await new BuscarTodasCarteirasUseCase(_uow).ExecutarAsync(_usuario.Id);

            CollectionAssert.AreEqual(new[] { "Ações", "Renda Fixa" }, lista.Select(c => c.Nome).ToArray());
        }

        [TestMethod]
        public async Task BuscarPorId_RetornaAtivaDoUsuario()
        {
            var carteira = await SemearCarteiraAsync("Ações");

            var obtida = await new BuscarCarteiraPorIdUseCase(_uow).ExecutarAsync(carteira.Id, _usuario.Id);

            Assert.IsNotNull(obtida);
            Assert.AreEqual("Ações", obtida.Nome);
        }

        [TestMethod]
        public async Task BuscarPorId_RetornaNuloParaInexistenteInativaOuDeOutroUsuario()
        {
            var inativa = await SemearCarteiraAsync("Antiga", ativa: false);
            var alheio = await SemearCarteiraAsync("Alheio", idUsuario: _outroUsuario.Id);
            var useCase = new BuscarCarteiraPorIdUseCase(_uow);

            Assert.IsNull(await useCase.ExecutarAsync(999, _usuario.Id));
            Assert.IsNull(await useCase.ExecutarAsync(inativa.Id, _usuario.Id));
            Assert.IsNull(await useCase.ExecutarAsync(alheio.Id, _usuario.Id));
        }
    }
}

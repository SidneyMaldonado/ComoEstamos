using ComoEstamos.Core;
using ComoEstamos.Core.UseCases.Credores;
using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    /// <summary>Os 5 casos de uso do CRUD de credor (Spec/07-Crud.md).</summary>
    [TestClass]
    public sealed class CrudCredorTests : RepositoryTestBase
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

        private async Task<Credor> SemearCredorAsync(string nome, int? idUsuario = null, bool ativo = true)
        {
            var credor = new Credor { IdUsuario = idUsuario ?? _usuario.Id, Nome = nome, Ativo = ativo };
            await SemearAsync(credor);
            return credor;
        }

        private async Task<List<Credor>> GravadosAsync()
        {
            await using var db = CriarContexto();
            return await db.Credores.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
        }

        private static async Task<ValidacaoException> RecusaCampoAsync(string campo, Func<Task> acao)
        {
            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(acao);
            Assert.IsTrue(ex.Erros.Any(e => e.Campo == campo), $"Esperava erro em {campo}: {ex.Message}");
            return ex;
        }

        // ---------- Incluir ----------

        [TestMethod]
        public async Task Incluir_GravaAtivoDoUsuarioRecebidoSemEspacos()
        {
            var credor = await new IncluirCredorUseCase(_uow).ExecutarAsync(
                new Credor { Id = 99, IdUsuario = _outroUsuario.Id, Nome = "  Banco ", Ativo = false },
                _usuario.Id);

            var gravado = (await GravadosAsync()).Single();
            Assert.AreEqual(credor.Id, gravado.Id);
            Assert.AreEqual(_usuario.Id, gravado.IdUsuario);
            Assert.AreEqual("Banco", gravado.Nome);
            Assert.IsTrue(gravado.Ativo);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public async Task Incluir_RecusaNomeVazioSemGravar(string nome)
        {
            await RecusaCampoAsync(nameof(Credor.Nome),
                () => new IncluirCredorUseCase(_uow).ExecutarAsync(new Credor { Nome = nome }, _usuario.Id));

            Assert.IsEmpty(await GravadosAsync());
        }

        [TestMethod]
        public async Task Incluir_RecusaNomeAcimaDe50CaracteresEAceita50()
        {
            await RecusaCampoAsync(nameof(Credor.Nome),
                () => new IncluirCredorUseCase(_uow).ExecutarAsync(new Credor { Nome = new string('x', 51) }, _usuario.Id));
            await new IncluirCredorUseCase(_uow).ExecutarAsync(new Credor { Nome = new string('x', 50) }, _usuario.Id);

            Assert.HasCount(1, await GravadosAsync());
        }

        [TestMethod]
        public async Task Incluir_RecusaNomeRepetidoSemDiferenciarMaiusculas()
        {
            await SemearCredorAsync("Banco");

            var ex = await RecusaCampoAsync(nameof(Credor.Nome),
                () => new IncluirCredorUseCase(_uow).ExecutarAsync(new Credor { Nome = " BANCO " }, _usuario.Id));

            Assert.AreEqual("Já existe um credor com este nome.", ex.Erros.Single().Mensagem);
            Assert.HasCount(1, await GravadosAsync());
        }

        [TestMethod]
        public async Task Incluir_AceitaNomeDeInativoOuDeOutroUsuario()
        {
            await SemearCredorAsync("Banco", ativo: false);
            await SemearCredorAsync("Loja", idUsuario: _outroUsuario.Id);

            await new IncluirCredorUseCase(_uow).ExecutarAsync(new Credor { Nome = "Banco" }, _usuario.Id);
            await new IncluirCredorUseCase(_uow).ExecutarAsync(new Credor { Nome = "Loja" }, _usuario.Id);

            Assert.HasCount(4, await GravadosAsync());
        }

        [TestMethod]
        public async Task Incluir_ObservacoesVaziasViramNulo()
        {
            await new IncluirCredorUseCase(_uow).ExecutarAsync(new Credor { Nome = "Banco", Observacoes = "   " }, _usuario.Id);

            Assert.IsNull((await GravadosAsync()).Single().Observacoes);
        }

        // ---------- Alterar ----------

        [TestMethod]
        public async Task Alterar_GravaOsCamposEditaveisEMantemOsDemaisCampos()
        {
            var original = await SemearCredorAsync("Banco");
            await using (var db = CriarContexto())
            {
                var c = await db.Credores.SingleAsync();
                c.Logo = [1, 2, 3];
                await db.SaveChangesAsync();
            }

            await new AlterarCredorUseCase(_uow).ExecutarAsync(new Credor
            {
                Id = original.Id,
                IdUsuario = _outroUsuario.Id,
                Nome = " Loja ",
                Observacoes = " Gerente: Ana ",
            }, _usuario.Id);

            var gravado = (await GravadosAsync()).Single();
            Assert.AreEqual("Loja", gravado.Nome);
            Assert.AreEqual("Gerente: Ana", gravado.Observacoes);
            Assert.AreEqual(_usuario.Id, gravado.IdUsuario);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, gravado.Logo);
            Assert.IsNotNull(gravado.DataAlteracao);
        }

        [TestMethod]
        public async Task Alterar_AceitaManterOProprioNome()
        {
            var credor = await SemearCredorAsync("Banco");

            await new AlterarCredorUseCase(_uow).ExecutarAsync(new Credor { Id = credor.Id, Nome = "BANCO" }, _usuario.Id);

            Assert.AreEqual("BANCO", (await GravadosAsync()).Single().Nome);
        }

        [TestMethod]
        public async Task Alterar_RecusaNomeDeOutroSemGravar()
        {
            await SemearCredorAsync("Loja");
            var credor = await SemearCredorAsync("Banco");

            await RecusaCampoAsync(nameof(Credor.Nome),
                () => new AlterarCredorUseCase(_uow).ExecutarAsync(new Credor { Id = credor.Id, Nome = "loja" }, _usuario.Id));

            Assert.AreEqual("Banco", (await GravadosAsync()).Last().Nome);
        }

        [TestMethod]
        public async Task Alterar_InexistenteOuDeOutroUsuarioNaoEncontra()
        {
            var alheio = await SemearCredorAsync("Alheio", idUsuario: _outroUsuario.Id);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
                () => new AlterarCredorUseCase(_uow).ExecutarAsync(new Credor { Id = 999, Nome = "X" }, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
                () => new AlterarCredorUseCase(_uow).ExecutarAsync(new Credor { Id = alheio.Id, Nome = "X" }, _usuario.Id));

            Assert.AreEqual("Alheio", (await GravadosAsync()).Single().Nome);
        }

        [TestMethod]
        public async Task Alterar_RecusaInativo()
        {
            var credor = await SemearCredorAsync("Banco", ativo: false);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => new AlterarCredorUseCase(_uow).ExecutarAsync(new Credor { Id = credor.Id, Nome = "X" }, _usuario.Id));
        }

        // ---------- Excluir ----------

        [TestMethod]
        public async Task Excluir_Desativa()
        {
            var credor = await SemearCredorAsync("Banco");

            await new ExcluirCredorUseCase(_uow).ExecutarAsync(credor.Id, _usuario.Id);

            Assert.IsFalse((await GravadosAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_RecusaComDividaAtivaSemDesativar()
        {
            var credor = await SemearCredorAsync("Banco");
            await SemearAsync(new Divida
            {
                IdCredor = credor.Id, Nome = "Financiamento",
                DataPrimeiroVencimento = new DateTime(2026, 10, 10), Valor = 100m, Ativo = true
            });

            var ex = await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => new ExcluirCredorUseCase(_uow).ExecutarAsync(credor.Id, _usuario.Id));

            Assert.AreEqual("Não é possível excluir: existem dívidas ativas deste credor.", ex.Message);
            Assert.IsTrue((await GravadosAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_DependenteInativoNaoImpede()
        {
            var credor = await SemearCredorAsync("Banco");
            await SemearAsync(new Divida
            {
                IdCredor = credor.Id, Nome = "Financiamento",
                DataPrimeiroVencimento = new DateTime(2026, 10, 10), Valor = 100m, Ativo = false
            });

            await new ExcluirCredorUseCase(_uow).ExecutarAsync(credor.Id, _usuario.Id);

            Assert.IsFalse((await GravadosAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_InexistenteOuDeOutroUsuarioNaoEncontra()
        {
            var alheio = await SemearCredorAsync("Alheio", idUsuario: _outroUsuario.Id);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => new ExcluirCredorUseCase(_uow).ExecutarAsync(999, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => new ExcluirCredorUseCase(_uow).ExecutarAsync(alheio.Id, _usuario.Id));

            Assert.IsTrue((await GravadosAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_RecusaJaExcluido()
        {
            var credor = await SemearCredorAsync("Banco", ativo: false);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => new ExcluirCredorUseCase(_uow).ExecutarAsync(credor.Id, _usuario.Id));
        }

        // ---------- Buscar ----------

        [TestMethod]
        public async Task BuscarTodos_RetornaSoAtivosDoUsuarioPorNome()
        {
            await SemearCredorAsync("Loja");
            await SemearCredorAsync("Banco");
            await SemearCredorAsync("Antigo", ativo: false);
            await SemearCredorAsync("Alheio", idUsuario: _outroUsuario.Id);

            var lista = await new BuscarTodosCredoresUseCase(_uow).ExecutarAsync(_usuario.Id);

            CollectionAssert.AreEqual(new[] { "Banco", "Loja" }, lista.Select(c => c.Nome).ToArray());
        }

        [TestMethod]
        public async Task BuscarPorId_RetornaAtivoDoUsuario()
        {
            var credor = await SemearCredorAsync("Banco");

            var obtido = await new BuscarCredorPorIdUseCase(_uow).ExecutarAsync(credor.Id, _usuario.Id);

            Assert.IsNotNull(obtido);
            Assert.AreEqual("Banco", obtido.Nome);
        }

        [TestMethod]
        public async Task BuscarPorId_RetornaNuloParaInexistenteInativoOuDeOutroUsuario()
        {
            var inativo = await SemearCredorAsync("Antigo", ativo: false);
            var alheio = await SemearCredorAsync("Alheio", idUsuario: _outroUsuario.Id);
            var useCase = new BuscarCredorPorIdUseCase(_uow);

            Assert.IsNull(await useCase.ExecutarAsync(999, _usuario.Id));
            Assert.IsNull(await useCase.ExecutarAsync(inativo.Id, _usuario.Id));
            Assert.IsNull(await useCase.ExecutarAsync(alheio.Id, _usuario.Id));
        }
    }
}

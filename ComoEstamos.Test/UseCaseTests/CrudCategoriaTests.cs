using ComoEstamos.Core;
using ComoEstamos.Core.UseCases.Categorias;
using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    /// <summary>Os 5 casos de uso do CRUD de categoria (Spec/07-Crud.md).</summary>
    [TestClass]
    public sealed class CrudCategoriaTests : RepositoryTestBase
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

        private async Task<Categoria> SemearCategoriaAsync(string nome, int? idUsuario = null, bool ativa = true)
        {
            var categoria = new Categoria { IdUsuario = idUsuario ?? _usuario.Id, Nome = nome, Ativo = ativa };
            await SemearAsync(categoria);
            return categoria;
        }

        private async Task<List<Categoria>> GravadasAsync()
        {
            await using var db = CriarContexto();
            return await db.Categorias.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
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
            var categoria = await new IncluirCategoriaUseCase(_uow).ExecutarAsync(
                new Categoria { Id = 99, IdUsuario = _outroUsuario.Id, Nome = "  Moradia ", Ativo = false },
                _usuario.Id);

            var gravada = (await GravadasAsync()).Single();
            Assert.AreEqual(categoria.Id, gravada.Id);
            Assert.AreEqual(_usuario.Id, gravada.IdUsuario);
            Assert.AreEqual("Moradia", gravada.Nome);
            Assert.IsTrue(gravada.Ativo);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public async Task Incluir_RecusaNomeVazioSemGravar(string nome)
        {
            await RecusaCampoAsync(nameof(Categoria.Nome),
                () => new IncluirCategoriaUseCase(_uow).ExecutarAsync(new Categoria { Nome = nome }, _usuario.Id));

            Assert.IsEmpty(await GravadasAsync());
        }

        [TestMethod]
        public async Task Incluir_RecusaNomeAcimaDe100CaracteresEAceita100()
        {
            await RecusaCampoAsync(nameof(Categoria.Nome),
                () => new IncluirCategoriaUseCase(_uow).ExecutarAsync(new Categoria { Nome = new string('x', 101) }, _usuario.Id));
            await new IncluirCategoriaUseCase(_uow).ExecutarAsync(new Categoria { Nome = new string('x', 100) }, _usuario.Id);

            Assert.HasCount(1, await GravadasAsync());
        }

        [TestMethod]
        public async Task Incluir_RecusaNomeRepetidoSemDiferenciarMaiusculas()
        {
            await SemearCategoriaAsync("Moradia");

            var ex = await RecusaCampoAsync(nameof(Categoria.Nome),
                () => new IncluirCategoriaUseCase(_uow).ExecutarAsync(new Categoria { Nome = " MORADIA " }, _usuario.Id));

            Assert.AreEqual("Já existe uma categoria com este nome.", ex.Erros.Single().Mensagem);
            Assert.HasCount(1, await GravadasAsync());
        }

        [TestMethod]
        public async Task Incluir_AceitaNomeDeInativaOuDeOutroUsuario()
        {
            await SemearCategoriaAsync("Moradia", ativa: false);
            await SemearCategoriaAsync("Lazer", idUsuario: _outroUsuario.Id);

            await new IncluirCategoriaUseCase(_uow).ExecutarAsync(new Categoria { Nome = "Moradia" }, _usuario.Id);
            await new IncluirCategoriaUseCase(_uow).ExecutarAsync(new Categoria { Nome = "Lazer" }, _usuario.Id);

            Assert.HasCount(4, await GravadasAsync());
        }

        // ---------- Alterar ----------

        [TestMethod]
        public async Task Alterar_GravaOsCamposEditaveisEMantemOsDemaisCampos()
        {
            var original = await SemearCategoriaAsync("Moradia");
            await using (var db = CriarContexto())
            {
                var c = await db.Categorias.SingleAsync();
                c.Imagem = [1, 2, 3];
                await db.SaveChangesAsync();
            }

            await new AlterarCategoriaUseCase(_uow).ExecutarAsync(new Categoria
            {
                Id = original.Id,
                IdUsuario = _outroUsuario.Id,
                Nome = " Lazer ",
            }, _usuario.Id);

            var gravada = (await GravadasAsync()).Single();
            Assert.AreEqual("Lazer", gravada.Nome);
            Assert.AreEqual(_usuario.Id, gravada.IdUsuario);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, gravada.Imagem);
            Assert.IsNotNull(gravada.DataAlteracao);
        }

        [TestMethod]
        public async Task Alterar_AceitaManterOProprioNome()
        {
            var categoria = await SemearCategoriaAsync("Moradia");

            await new AlterarCategoriaUseCase(_uow).ExecutarAsync(new Categoria { Id = categoria.Id, Nome = "MORADIA" }, _usuario.Id);

            Assert.AreEqual("MORADIA", (await GravadasAsync()).Single().Nome);
        }

        [TestMethod]
        public async Task Alterar_RecusaNomeDeOutraSemGravar()
        {
            await SemearCategoriaAsync("Lazer");
            var categoria = await SemearCategoriaAsync("Moradia");

            await RecusaCampoAsync(nameof(Categoria.Nome),
                () => new AlterarCategoriaUseCase(_uow).ExecutarAsync(new Categoria { Id = categoria.Id, Nome = "lazer" }, _usuario.Id));

            Assert.AreEqual("Moradia", (await GravadasAsync()).Last().Nome);
        }

        [TestMethod]
        public async Task Alterar_InexistenteOuDeOutroUsuarioNaoEncontra()
        {
            var alheio = await SemearCategoriaAsync("Alheio", idUsuario: _outroUsuario.Id);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
                () => new AlterarCategoriaUseCase(_uow).ExecutarAsync(new Categoria { Id = 999, Nome = "X" }, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
                () => new AlterarCategoriaUseCase(_uow).ExecutarAsync(new Categoria { Id = alheio.Id, Nome = "X" }, _usuario.Id));

            Assert.AreEqual("Alheio", (await GravadasAsync()).Single().Nome);
        }

        [TestMethod]
        public async Task Alterar_RecusaInativa()
        {
            var categoria = await SemearCategoriaAsync("Moradia", ativa: false);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => new AlterarCategoriaUseCase(_uow).ExecutarAsync(new Categoria { Id = categoria.Id, Nome = "X" }, _usuario.Id));
        }

        // ---------- Excluir ----------

        [TestMethod]
        public async Task Excluir_Desativa()
        {
            var categoria = await SemearCategoriaAsync("Moradia");

            await new ExcluirCategoriaUseCase(_uow).ExecutarAsync(categoria.Id, _usuario.Id);

            Assert.IsFalse((await GravadasAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_RecusaComDividaAtivaSemDesativar()
        {
            var categoria = await SemearCategoriaAsync("Moradia");
            await SemearAsync(new Divida
            {
                IdCategoria = categoria.Id, Nome = "Financiamento",
                DataPrimeiroVencimento = new DateTime(2026, 10, 10), Valor = 100m, Ativo = true
            });

            var ex = await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => new ExcluirCategoriaUseCase(_uow).ExecutarAsync(categoria.Id, _usuario.Id));

            Assert.AreEqual("Não é possível excluir: existem dívidas ativas nesta categoria.", ex.Message);
            Assert.IsTrue((await GravadasAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_DependenteInativoNaoImpede()
        {
            var categoria = await SemearCategoriaAsync("Moradia");
            await SemearAsync(new Divida
            {
                IdCategoria = categoria.Id, Nome = "Financiamento",
                DataPrimeiroVencimento = new DateTime(2026, 10, 10), Valor = 100m, Ativo = false
            });

            await new ExcluirCategoriaUseCase(_uow).ExecutarAsync(categoria.Id, _usuario.Id);

            Assert.IsFalse((await GravadasAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_InexistenteOuDeOutroUsuarioNaoEncontra()
        {
            var alheio = await SemearCategoriaAsync("Alheio", idUsuario: _outroUsuario.Id);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => new ExcluirCategoriaUseCase(_uow).ExecutarAsync(999, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => new ExcluirCategoriaUseCase(_uow).ExecutarAsync(alheio.Id, _usuario.Id));

            Assert.IsTrue((await GravadasAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_RecusaJaExcluida()
        {
            var categoria = await SemearCategoriaAsync("Moradia", ativa: false);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => new ExcluirCategoriaUseCase(_uow).ExecutarAsync(categoria.Id, _usuario.Id));
        }

        // ---------- Buscar ----------

        [TestMethod]
        public async Task BuscarTodas_RetornaSoAtivasDoUsuarioPorNome()
        {
            await SemearCategoriaAsync("Lazer");
            await SemearCategoriaAsync("Moradia");
            await SemearCategoriaAsync("Antiga", ativa: false);
            await SemearCategoriaAsync("Alheio", idUsuario: _outroUsuario.Id);

            var lista = await new BuscarTodasCategoriasUseCase(_uow).ExecutarAsync(_usuario.Id);

            CollectionAssert.AreEqual(new[] { "Lazer", "Moradia" }, lista.Select(c => c.Nome).ToArray());
        }

        [TestMethod]
        public async Task BuscarPorId_RetornaAtivaDoUsuario()
        {
            var categoria = await SemearCategoriaAsync("Moradia");

            var obtida = await new BuscarCategoriaPorIdUseCase(_uow).ExecutarAsync(categoria.Id, _usuario.Id);

            Assert.IsNotNull(obtida);
            Assert.AreEqual("Moradia", obtida.Nome);
        }

        [TestMethod]
        public async Task BuscarPorId_RetornaNuloParaInexistenteInativaOuDeOutroUsuario()
        {
            var inativa = await SemearCategoriaAsync("Antiga", ativa: false);
            var alheio = await SemearCategoriaAsync("Alheio", idUsuario: _outroUsuario.Id);
            var useCase = new BuscarCategoriaPorIdUseCase(_uow);

            Assert.IsNull(await useCase.ExecutarAsync(999, _usuario.Id));
            Assert.IsNull(await useCase.ExecutarAsync(inativa.Id, _usuario.Id));
            Assert.IsNull(await useCase.ExecutarAsync(alheio.Id, _usuario.Id));
        }
    }
}

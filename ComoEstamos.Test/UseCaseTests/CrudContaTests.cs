using ComoEstamos.Core;
using ComoEstamos.Core.UseCases.Contas;
using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    /// <summary>Os 5 casos de uso do CRUD de conta (Spec/07-Crud.md).</summary>
    [TestClass]
    public sealed class CrudContaTests : RepositoryTestBase
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

        private async Task<Conta> SemearContaAsync(string nome, int? idUsuario = null, bool ativa = true, decimal saldo = 0m)
        {
            var conta = new Conta { IdUsuario = idUsuario ?? _usuario.Id, Nome = nome, Saldo = saldo, Ativo = ativa };
            await SemearAsync(conta);
            return conta;
        }

        private async Task<List<Conta>> GravadasAsync()
        {
            await using var db = CriarContexto();
            return await db.Contas.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
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
            var conta = await new IncluirContaUseCase(_uow).ExecutarAsync(
                new Conta { Id = 99, IdUsuario = _outroUsuario.Id, Nome = "  Nubank ", Saldo = -10.5m, Ativo = false },
                _usuario.Id);

            var gravada = (await GravadasAsync()).Single();
            Assert.AreEqual(conta.Id, gravada.Id);
            Assert.AreEqual(_usuario.Id, gravada.IdUsuario);
            Assert.AreEqual("Nubank", gravada.Nome);
            Assert.AreEqual(-10.5m, gravada.Saldo);
            Assert.IsTrue(gravada.Ativo);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public async Task Incluir_RecusaNomeVazioSemGravar(string nome)
        {
            await RecusaCampoAsync(nameof(Conta.Nome),
                () => new IncluirContaUseCase(_uow).ExecutarAsync(new Conta { Nome = nome }, _usuario.Id));

            Assert.IsEmpty(await GravadasAsync());
        }

        [TestMethod]
        public async Task Incluir_RecusaNomeAcimaDe100CaracteresEAceita100()
        {
            await RecusaCampoAsync(nameof(Conta.Nome),
                () => new IncluirContaUseCase(_uow).ExecutarAsync(new Conta { Nome = new string('x', 101) }, _usuario.Id));
            await new IncluirContaUseCase(_uow).ExecutarAsync(new Conta { Nome = new string('x', 100) }, _usuario.Id);

            Assert.HasCount(1, await GravadasAsync());
        }

        [TestMethod]
        [DataRow("10.123")]
        [DataRow("100000000")]
        [DataRow("-100000000")]
        public async Task Incluir_RecusaSaldoForaDoDominioSemGravar(string saldo)
        {
            await RecusaCampoAsync(nameof(Conta.Saldo), () => new IncluirContaUseCase(_uow).ExecutarAsync(
                new Conta { Nome = "Nubank", Saldo = decimal.Parse(saldo, System.Globalization.CultureInfo.InvariantCulture) },
                _usuario.Id));

            Assert.IsEmpty(await GravadasAsync());
        }

        [TestMethod]
        public async Task Incluir_DevolveTodosOsErrosJuntos()
        {
            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(
                () => new IncluirContaUseCase(_uow).ExecutarAsync(new Conta { Nome = "", Saldo = 0.001m }, _usuario.Id));

            CollectionAssert.AreEquivalent(new[] { nameof(Conta.Nome), nameof(Conta.Saldo) }, ex.Erros.Select(e => e.Campo).ToArray());
        }

        [TestMethod]
        public async Task Incluir_RecusaNomeRepetidoSemDiferenciarMaiusculas()
        {
            await SemearContaAsync("Nubank");

            var ex = await RecusaCampoAsync(nameof(Conta.Nome),
                () => new IncluirContaUseCase(_uow).ExecutarAsync(new Conta { Nome = " NUBANK " }, _usuario.Id));

            Assert.AreEqual("Já existe uma conta com este nome.", ex.Erros.Single().Mensagem);
            Assert.HasCount(1, await GravadasAsync());
        }

        [TestMethod]
        public async Task Incluir_AceitaNomeDeContaInativaOuDeOutroUsuario()
        {
            await SemearContaAsync("Nubank", ativa: false);
            await SemearContaAsync("Inter", idUsuario: _outroUsuario.Id);

            await new IncluirContaUseCase(_uow).ExecutarAsync(new Conta { Nome = "Nubank" }, _usuario.Id);
            await new IncluirContaUseCase(_uow).ExecutarAsync(new Conta { Nome = "Inter" }, _usuario.Id);

            Assert.HasCount(4, await GravadasAsync());
        }

        // ---------- Alterar ----------

        [TestMethod]
        public async Task Alterar_GravaNomeESaldoEMantemOsDemaisCampos()
        {
            var original = await SemearContaAsync("Nubank", saldo: 10m);
            await using (var db = CriarContexto())
            {
                var c = await db.Contas.SingleAsync();
                c.Imagem = [1, 2, 3];
                await db.SaveChangesAsync();
            }

            await new AlterarContaUseCase(_uow).ExecutarAsync(
                new Conta { Id = original.Id, IdUsuario = _outroUsuario.Id, Nome = " Nubank PJ ", Saldo = 20.5m }, _usuario.Id);

            var gravada = (await GravadasAsync()).Single();
            Assert.AreEqual("Nubank PJ", gravada.Nome);
            Assert.AreEqual(20.5m, gravada.Saldo);
            Assert.AreEqual(_usuario.Id, gravada.IdUsuario);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, gravada.Imagem);
            Assert.IsNotNull(gravada.DataAlteracao);
        }

        [TestMethod]
        public async Task Alterar_SaldoDiferenteGravaHistorico()
        {
            var conta = await SemearContaAsync("Nubank", saldo: 10m);

            await new AlterarContaUseCase(_uow).ExecutarAsync(new Conta { Id = conta.Id, Nome = "Nubank", Saldo = -3.5m }, _usuario.Id);

            await using var db = CriarContexto();
            var historico = await db.HistoricosSaldo.AsNoTracking().SingleAsync();
            Assert.AreEqual(conta.Id, historico.IdConta);
            Assert.AreEqual(-3.5m, historico.Saldo);
        }

        [TestMethod]
        public async Task Alterar_MesmoSaldoNaoGravaHistorico()
        {
            var conta = await SemearContaAsync("Nubank", saldo: 10m);

            await new AlterarContaUseCase(_uow).ExecutarAsync(new Conta { Id = conta.Id, Nome = "Nubank PJ", Saldo = 10m }, _usuario.Id);

            await using var db = CriarContexto();
            Assert.IsFalse(await db.HistoricosSaldo.AnyAsync());
        }

        [TestMethod]
        public async Task Alterar_AceitaManterOProprioNome()
        {
            var conta = await SemearContaAsync("Nubank");

            await new AlterarContaUseCase(_uow).ExecutarAsync(new Conta { Id = conta.Id, Nome = "NUBANK", Saldo = 1m }, _usuario.Id);

            Assert.AreEqual("NUBANK", (await GravadasAsync()).Single().Nome);
        }

        [TestMethod]
        public async Task Alterar_RecusaNomeDeOutraContaSemGravar()
        {
            await SemearContaAsync("Inter");
            var conta = await SemearContaAsync("Nubank");

            await RecusaCampoAsync(nameof(Conta.Nome),
                () => new AlterarContaUseCase(_uow).ExecutarAsync(new Conta { Id = conta.Id, Nome = "inter" }, _usuario.Id));

            Assert.AreEqual("Nubank", (await GravadasAsync()).Last().Nome);
        }

        [TestMethod]
        public async Task Alterar_RecusaDominioSemGravar()
        {
            var conta = await SemearContaAsync("Nubank", saldo: 10m);

            await RecusaCampoAsync(nameof(Conta.Saldo),
                () => new AlterarContaUseCase(_uow).ExecutarAsync(new Conta { Id = conta.Id, Nome = "Outro", Saldo = 1.001m }, _usuario.Id));

            var gravada = (await GravadasAsync()).Single();
            Assert.AreEqual("Nubank", gravada.Nome);
            Assert.AreEqual(10m, gravada.Saldo);
            await using var db = CriarContexto();
            Assert.IsFalse(await db.HistoricosSaldo.AnyAsync());
        }

        [TestMethod]
        public async Task Alterar_InexistenteOuDeOutroUsuarioNaoEncontra()
        {
            var alheia = await SemearContaAsync("Alheia", idUsuario: _outroUsuario.Id);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
                () => new AlterarContaUseCase(_uow).ExecutarAsync(new Conta { Id = 999, Nome = "X" }, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
                () => new AlterarContaUseCase(_uow).ExecutarAsync(new Conta { Id = alheia.Id, Nome = "X" }, _usuario.Id));

            Assert.AreEqual("Alheia", (await GravadasAsync()).Single().Nome);
        }

        [TestMethod]
        public async Task Alterar_RecusaContaInativa()
        {
            var conta = await SemearContaAsync("Nubank", ativa: false);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => new AlterarContaUseCase(_uow).ExecutarAsync(new Conta { Id = conta.Id, Nome = "X" }, _usuario.Id));
        }

        // ---------- Excluir ----------

        [TestMethod]
        public async Task Excluir_DesativaAConta()
        {
            var conta = await SemearContaAsync("Nubank");

            await new ExcluirContaUseCase(_uow).ExecutarAsync(conta.Id, _usuario.Id);

            Assert.IsFalse((await GravadasAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_DesativaOHistoricoDeSaldoSoDaConta()
        {
            var conta = await SemearContaAsync("Nubank");
            var outra = await SemearContaAsync("Inter");
            await SemearAsync(
                new HistoricoSaldo { IdConta = conta.Id, Saldo = 100m },
                new HistoricoSaldo { IdConta = conta.Id, Saldo = 200m },
                new HistoricoSaldo { IdConta = outra.Id, Saldo = 300m });

            await new ExcluirContaUseCase(_uow).ExecutarAsync(conta.Id, _usuario.Id);

            await using var db = CriarContexto();
            var historicos = await db.HistoricosSaldo.AsNoTracking().ToListAsync();
            Assert.IsTrue(historicos.Where(h => h.IdConta == conta.Id).All(h => !h.Ativo));
            Assert.IsTrue(historicos.Single(h => h.IdConta == outra.Id).Ativo);
        }

        [TestMethod]
        public async Task Excluir_RecusadaNaoDesativaOHistorico()
        {
            var conta = await SemearContaAsync("Nubank");
            await SemearAsync(NovaDivida(conta.Id), new HistoricoSaldo { IdConta = conta.Id, Saldo = 100m });

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => new ExcluirContaUseCase(_uow).ExecutarAsync(conta.Id, _usuario.Id));

            await using var db = CriarContexto();
            Assert.IsTrue((await db.HistoricosSaldo.AsNoTracking().SingleAsync()).Ativo);
        }

        [TestMethod]
        public async Task Excluir_RecusaComDividaAtivaSemDesativar()
        {
            var conta = await SemearContaAsync("Nubank");
            await SemearAsync(NovaDivida(conta.Id));

            var ex = await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => new ExcluirContaUseCase(_uow).ExecutarAsync(conta.Id, _usuario.Id));

            Assert.AreEqual("Não é possível excluir: existem dívidas ativas nesta conta.", ex.Message);
            Assert.IsTrue((await GravadasAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_RecusaComParcelaAtivaSemDesativar()
        {
            var conta = await SemearContaAsync("Nubank");
            var outra = await SemearContaAsync("Inter");
            var divida = NovaDivida(outra.Id);
            await SemearAsync(divida);
            var categoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Moradia" };
            await SemearAsync(categoria);
            await SemearAsync(new Parcela
            {
                IdDivida = divida.Id, IdConta = conta.Id, IdCategoria = categoria.Id,
                Descricao = "1/1", Valor = 10m, DataVencimento = new DateTime(2026, 10, 10)
            });

            var ex = await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => new ExcluirContaUseCase(_uow).ExecutarAsync(conta.Id, _usuario.Id));

            Assert.AreEqual("Não é possível excluir: existem parcelas ativas nesta conta.", ex.Message);
            Assert.IsTrue((await GravadasAsync()).All(c => c.Ativo));
        }

        [TestMethod]
        public async Task Excluir_DividaInativaNaoImpede()
        {
            var conta = await SemearContaAsync("Nubank");
            var divida = NovaDivida(conta.Id);
            divida.Ativo = false;
            await SemearAsync(divida);

            await new ExcluirContaUseCase(_uow).ExecutarAsync(conta.Id, _usuario.Id);

            Assert.IsFalse((await GravadasAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_InexistenteOuDeOutroUsuarioNaoEncontra()
        {
            var alheia = await SemearContaAsync("Alheia", idUsuario: _outroUsuario.Id);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => new ExcluirContaUseCase(_uow).ExecutarAsync(999, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => new ExcluirContaUseCase(_uow).ExecutarAsync(alheia.Id, _usuario.Id));

            Assert.IsTrue((await GravadasAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_RecusaContaJaExcluida()
        {
            var conta = await SemearContaAsync("Nubank", ativa: false);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => new ExcluirContaUseCase(_uow).ExecutarAsync(conta.Id, _usuario.Id));
        }

        private Divida NovaDivida(int idConta) => new()
        {
            IdUsuario = _usuario.Id,
            IdConta = idConta,
            Nome = "Financiamento",
            DataPrimeiroVencimento = new DateTime(2026, 10, 10),
            Valor = 100m
        };

        // ---------- Buscar ----------

        [TestMethod]
        public async Task BuscarTodas_RetornaSoAsAtivasDoUsuarioPorNome()
        {
            await SemearContaAsync("Nubank");
            await SemearContaAsync("Inter");
            await SemearContaAsync("Antiga", ativa: false);
            await SemearContaAsync("Alheia", idUsuario: _outroUsuario.Id);

            var contas = await new BuscarTodasContasUseCase(_uow).ExecutarAsync(_usuario.Id);

            CollectionAssert.AreEqual(new[] { "Inter", "Nubank" }, contas.Select(c => c.Nome).ToArray());
        }

        [TestMethod]
        public async Task BuscarPorId_RetornaAContaAtivaDoUsuario()
        {
            var conta = await SemearContaAsync("Nubank", saldo: 5m);

            var obtida = await new BuscarContaPorIdUseCase(_uow).ExecutarAsync(conta.Id, _usuario.Id);

            Assert.IsNotNull(obtida);
            Assert.AreEqual("Nubank", obtida.Nome);
            Assert.AreEqual(5m, obtida.Saldo);
        }

        [TestMethod]
        public async Task BuscarPorId_RetornaNuloParaInexistenteInativaOuDeOutroUsuario()
        {
            var inativa = await SemearContaAsync("Antiga", ativa: false);
            var alheia = await SemearContaAsync("Alheia", idUsuario: _outroUsuario.Id);
            var useCase = new BuscarContaPorIdUseCase(_uow);

            Assert.IsNull(await useCase.ExecutarAsync(999, _usuario.Id));
            Assert.IsNull(await useCase.ExecutarAsync(inativa.Id, _usuario.Id));
            Assert.IsNull(await useCase.ExecutarAsync(alheia.Id, _usuario.Id));
        }
    }
}

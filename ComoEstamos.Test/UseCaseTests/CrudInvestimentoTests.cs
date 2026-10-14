using ComoEstamos.Core;
using ComoEstamos.Core.UseCases.Investimentos;
using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.UseCaseTests
{
    /// <summary>
    /// Os 5 casos de uso do CRUD de investimento (Spec/07-Crud.md). O investimento não tem
    /// id_usuario: o dono é o da carteira, e o nome é único dentro da carteira.
    /// </summary>
    [TestClass]
    public sealed class CrudInvestimentoTests : RepositoryTestBase
    {
        private UnitOfWorkFactory _uow = null!;
        private Usuario _usuario = null!;
        private Carteira _acoes = null!;
        private Carteira _rendaFixa = null!;
        private Carteira _alheia = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _uow = new UnitOfWorkFactory(ContextFactory);
            _usuario = await CriarUsuarioAsync();
            var outroUsuario = await CriarUsuarioAsync("outro@teste.com");
            _acoes = new Carteira { IdUsuario = _usuario.Id, Nome = "Ações" };
            _rendaFixa = new Carteira { IdUsuario = _usuario.Id, Nome = "Renda Fixa" };
            _alheia = new Carteira { IdUsuario = outroUsuario.Id, Nome = "Alheia" };
            await SemearAsync(_acoes, _rendaFixa, _alheia);
        }

        private async Task<Investimento> SemearInvestimentoAsync(string nome, Carteira? carteira = null, bool ativo = true)
        {
            var investimento = new Investimento
            {
                IdCarteira = (carteira ?? _acoes).Id, Nome = nome, Quantidade = 100m, Cotacao = 32.5m, Ativo = ativo
            };
            await SemearAsync(investimento);
            return investimento;
        }

        private Investimento Novo(string nome = "PETR4", int? idCarteira = null) => new()
        {
            IdCarteira = idCarteira ?? _acoes.Id, Nome = nome, Quantidade = 10.123456m, Cotacao = 32.5m
        };

        private async Task<List<Investimento>> GravadosAsync()
        {
            await using var db = CriarContexto();
            return await db.Investimentos.AsNoTracking().OrderBy(i => i.Id).ToListAsync();
        }

        private static async Task<ValidacaoException> RecusaCampoAsync(string campo, Func<Task> acao)
        {
            var ex = await Assert.ThrowsExactlyAsync<ValidacaoException>(acao);
            Assert.IsTrue(ex.Erros.Any(e => e.Campo == campo), $"Esperava erro em {campo}: {ex.Message}");
            return ex;
        }

        // ---------- Incluir ----------

        [TestMethod]
        public async Task Incluir_GravaAtivoNormalizado()
        {
            var novo = Novo();
            novo.Nome = " PETR4 ";
            novo.Observacao = "   ";
            novo.Ativo = false;

            await new IncluirInvestimentoUseCase(_uow).ExecutarAsync(novo, _usuario.Id);

            var gravado = (await GravadosAsync()).Single();
            Assert.AreEqual("PETR4", gravado.Nome);
            Assert.AreEqual(_acoes.Id, gravado.IdCarteira);
            Assert.AreEqual(10.123456m, gravado.Quantidade);
            Assert.IsNull(gravado.Observacao);
            Assert.IsTrue(gravado.Ativo);
        }

        [TestMethod]
        public async Task Incluir_RecusaSemCarteiraSemGravar()
        {
            var ex = await RecusaCampoAsync(nameof(Investimento.IdCarteira),
                () => new IncluirInvestimentoUseCase(_uow).ExecutarAsync(Novo(idCarteira: 0), _usuario.Id));

            Assert.AreEqual("Escolha a carteira.", ex.Erros.Single().Mensagem);
            Assert.IsEmpty(await GravadosAsync());
        }

        [TestMethod]
        public async Task Incluir_RecusaCarteiraInexistenteInativaOuDeOutroUsuario()
        {
            var inativa = new Carteira { IdUsuario = _usuario.Id, Nome = "Antiga", Ativo = false };
            await SemearAsync(inativa);
            var useCase = new IncluirInvestimentoUseCase(_uow);

            foreach (var idCarteira in new[] { 999, inativa.Id, _alheia.Id })
                await RecusaCampoAsync(nameof(Investimento.IdCarteira), () => useCase.ExecutarAsync(Novo(idCarteira: idCarteira), _usuario.Id));

            Assert.IsEmpty(await GravadosAsync());
        }

        [TestMethod]
        public async Task Incluir_RecusaCadaCampoForaDoDominioSemGravar()
        {
            var useCase = new IncluirInvestimentoUseCase(_uow);
            var casos = new (string Campo, Action<Investimento> Alterar)[]
            {
                (nameof(Investimento.Nome), i => i.Nome = " "),
                (nameof(Investimento.Nome), i => i.Nome = new string('x', 51)),
                (nameof(Investimento.Quantidade), i => i.Quantidade = -1m),
                (nameof(Investimento.Quantidade), i => i.Quantidade = 0.0000001m),
                (nameof(Investimento.Cotacao), i => i.Cotacao = -0.01m),
                (nameof(Investimento.Cotacao), i => i.Cotacao = Limites.Decimal18x6 + 1m),
                (nameof(Investimento.Observacao), i => i.Observacao = new string('x', 501)),
            };

            foreach (var (campo, alterar) in casos)
            {
                var investimento = Novo();
                alterar(investimento);
                await RecusaCampoAsync(campo, () => useCase.ExecutarAsync(investimento, _usuario.Id));
            }

            Assert.IsEmpty(await GravadosAsync());
        }

        [TestMethod]
        public async Task Incluir_AceitaOsLimitesDoDominio()
        {
            var investimento = Novo(new string('x', 50));
            investimento.Quantidade = 0m;
            investimento.Cotacao = Limites.Decimal18x6;
            investimento.Observacao = new string('x', 500);

            await new IncluirInvestimentoUseCase(_uow).ExecutarAsync(investimento, _usuario.Id);

            Assert.HasCount(1, await GravadosAsync());
        }

        [TestMethod]
        public async Task Incluir_RecusaNomeRepetidoNaMesmaCarteiraEAceitaEmOutra()
        {
            await SemearInvestimentoAsync("PETR4");
            var useCase = new IncluirInvestimentoUseCase(_uow);

            var ex = await RecusaCampoAsync(nameof(Investimento.Nome), () => useCase.ExecutarAsync(Novo("petr4"), _usuario.Id));
            await useCase.ExecutarAsync(Novo("PETR4", _rendaFixa.Id), _usuario.Id);

            Assert.AreEqual("Já existe um investimento com este nome nesta carteira.", ex.Erros.Single().Mensagem);
            Assert.HasCount(2, await GravadosAsync());
        }

        // ---------- Alterar ----------

        [TestMethod]
        public async Task Alterar_GravaOsCamposEditaveisInclusiveACarteira()
        {
            var original = await SemearInvestimentoAsync("PETR4");

            await new AlterarInvestimentoUseCase(_uow).ExecutarAsync(new Investimento
            {
                Id = original.Id, IdCarteira = _rendaFixa.Id, Nome = "CDB", Quantidade = 1.5m, Cotacao = 1000m, Observacao = " 120% CDI "
            }, _usuario.Id);

            var gravado = (await GravadosAsync()).Single();
            Assert.AreEqual(_rendaFixa.Id, gravado.IdCarteira);
            Assert.AreEqual("CDB", gravado.Nome);
            Assert.AreEqual(1.5m, gravado.Quantidade);
            Assert.AreEqual(1000m, gravado.Cotacao);
            Assert.AreEqual("120% CDI", gravado.Observacao);
        }

        [TestMethod]
        public async Task Alterar_AceitaManterOProprioNome()
        {
            var investimento = await SemearInvestimentoAsync("PETR4");

            await new AlterarInvestimentoUseCase(_uow).ExecutarAsync(
                new Investimento { Id = investimento.Id, IdCarteira = _acoes.Id, Nome = "petr4" }, _usuario.Id);

            Assert.AreEqual("petr4", (await GravadosAsync()).Single().Nome);
        }

        [TestMethod]
        public async Task Alterar_RecusaMoverParaCarteiraDeOutroUsuarioSemGravar()
        {
            var investimento = await SemearInvestimentoAsync("PETR4");

            await RecusaCampoAsync(nameof(Investimento.IdCarteira), () => new AlterarInvestimentoUseCase(_uow).ExecutarAsync(
                new Investimento { Id = investimento.Id, IdCarteira = _alheia.Id, Nome = "PETR4" }, _usuario.Id));

            Assert.AreEqual(_acoes.Id, (await GravadosAsync()).Single().IdCarteira);
        }

        [TestMethod]
        public async Task Alterar_RecusaNomeDeOutroInvestimentoDaCarteiraDestino()
        {
            await SemearInvestimentoAsync("CDB", _rendaFixa);
            var investimento = await SemearInvestimentoAsync("PETR4");

            await RecusaCampoAsync(nameof(Investimento.Nome), () => new AlterarInvestimentoUseCase(_uow).ExecutarAsync(
                new Investimento { Id = investimento.Id, IdCarteira = _rendaFixa.Id, Nome = "cdb" }, _usuario.Id));

            Assert.AreEqual("PETR4", (await GravadosAsync()).Last().Nome);
        }

        [TestMethod]
        public async Task Alterar_InexistenteOuDeOutroUsuarioNaoEncontra()
        {
            var alheio = await SemearInvestimentoAsync("BBAS3", _alheia);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => new AlterarInvestimentoUseCase(_uow).ExecutarAsync(
                new Investimento { Id = 999, IdCarteira = _acoes.Id, Nome = "X" }, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => new AlterarInvestimentoUseCase(_uow).ExecutarAsync(
                new Investimento { Id = alheio.Id, IdCarteira = _acoes.Id, Nome = "X" }, _usuario.Id));

            Assert.AreEqual(_alheia.Id, (await GravadosAsync()).Single().IdCarteira);
        }

        [TestMethod]
        public async Task Alterar_RecusaInativo()
        {
            var investimento = await SemearInvestimentoAsync("PETR4", ativo: false);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => new AlterarInvestimentoUseCase(_uow).ExecutarAsync(
                new Investimento { Id = investimento.Id, IdCarteira = _acoes.Id, Nome = "X" }, _usuario.Id));
        }

        // ---------- Excluir ----------

        [TestMethod]
        public async Task Excluir_Desativa()
        {
            var investimento = await SemearInvestimentoAsync("PETR4");

            await new ExcluirInvestimentoUseCase(_uow).ExecutarAsync(investimento.Id, _usuario.Id);

            Assert.IsFalse((await GravadosAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_RecusaComOperacaoAtivaSemDesativar()
        {
            var investimento = await SemearInvestimentoAsync("PETR4");
            await SemearAsync(new Operacao { IdInvestimento = investimento.Id, EhCompra = true, Quantidade = 1m, ValorTotal = 32m });

            var ex = await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => new ExcluirInvestimentoUseCase(_uow).ExecutarAsync(investimento.Id, _usuario.Id));

            Assert.AreEqual("Não é possível excluir: existem operações ativas deste investimento.", ex.Message);
            Assert.IsTrue((await GravadosAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_RecusaComHistoricoAtivoSemDesativar()
        {
            var investimento = await SemearInvestimentoAsync("PETR4");
            await SemearAsync(new Historico { IdInvestimento = investimento.Id, NomeInvestimento = "PETR4", Quantidade = 1m, Cotacao = 32m });

            var ex = await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(
                () => new ExcluirInvestimentoUseCase(_uow).ExecutarAsync(investimento.Id, _usuario.Id));

            Assert.AreEqual("Não é possível excluir: existem históricos ativos deste investimento.", ex.Message);
            Assert.IsTrue((await GravadosAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_DependenteInativoNaoImpede()
        {
            var investimento = await SemearInvestimentoAsync("PETR4");
            await SemearAsync(new Operacao { IdInvestimento = investimento.Id, EhCompra = true, Quantidade = 1m, ValorTotal = 32m, Ativo = false });

            await new ExcluirInvestimentoUseCase(_uow).ExecutarAsync(investimento.Id, _usuario.Id);

            Assert.IsFalse((await GravadosAsync()).Single().Ativo);
        }

        [TestMethod]
        public async Task Excluir_InexistenteOuDeOutroUsuarioNaoEncontra()
        {
            var alheio = await SemearInvestimentoAsync("BBAS3", _alheia);

            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => new ExcluirInvestimentoUseCase(_uow).ExecutarAsync(999, _usuario.Id));
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => new ExcluirInvestimentoUseCase(_uow).ExecutarAsync(alheio.Id, _usuario.Id));

            Assert.IsTrue((await GravadosAsync()).Single().Ativo);
        }

        // ---------- Buscar ----------

        [TestMethod]
        public async Task BuscarTodos_RetornaSoAtivosDasCarteirasDoUsuarioComCarteira()
        {
            await SemearInvestimentoAsync("VALE3");
            await SemearInvestimentoAsync("CDB", _rendaFixa);
            await SemearInvestimentoAsync("PETR4");
            await SemearInvestimentoAsync("OIBR3", ativo: false);
            await SemearInvestimentoAsync("BBAS3", _alheia);

            var lista = await new BuscarTodosInvestimentosUseCase(_uow).ExecutarAsync(_usuario.Id);

            CollectionAssert.AreEqual(new[] { "PETR4", "VALE3", "CDB" }, lista.Select(i => i.Nome).ToArray());
            Assert.AreEqual("Renda Fixa", lista[2].Carteira?.Nome);
        }

        [TestMethod]
        public async Task BuscarPorId_RetornaOInvestimentoComACarteira()
        {
            var investimento = await SemearInvestimentoAsync("PETR4");

            var obtido = await new BuscarInvestimentoPorIdUseCase(_uow).ExecutarAsync(investimento.Id, _usuario.Id);

            Assert.IsNotNull(obtido);
            Assert.AreEqual("PETR4", obtido.Nome);
            Assert.AreEqual("Ações", obtido.Carteira?.Nome);
        }

        [TestMethod]
        public async Task BuscarPorId_RetornaNuloParaInexistenteInativoOuDeOutroUsuario()
        {
            var inativo = await SemearInvestimentoAsync("OIBR3", ativo: false);
            var alheio = await SemearInvestimentoAsync("BBAS3", _alheia);
            var useCase = new BuscarInvestimentoPorIdUseCase(_uow);

            Assert.IsNull(await useCase.ExecutarAsync(999, _usuario.Id));
            Assert.IsNull(await useCase.ExecutarAsync(inativo.Id, _usuario.Id));
            Assert.IsNull(await useCase.ExecutarAsync(alheio.Id, _usuario.Id));
        }
    }
}

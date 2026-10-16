using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;

namespace ComoEstamos.Test.UseCaseTests
{
    [TestClass]
    public sealed class BuscarDividasUseCaseTests : RepositoryTestBase
    {
        private BuscarDividasUseCase _useCase = null!;
        private Usuario _usuario = null!;
        private Conta _conta = null!;
        private Categoria _categoria = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _useCase = new BuscarDividasUseCase(new UnitOfWorkFactory(ContextFactory));
            _usuario = await CriarUsuarioAsync();
            _conta = new Conta { IdUsuario = _usuario.Id, Nome = "Corrente" };
            _categoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Compras" };
            await SemearAsync(_conta, _categoria);
        }

        /// <summary>
        /// Grava uma dívida com uma parcela por vencimento informado (todas de R$ 100);
        /// as <paramref name="pagas"/> primeiras ficam pagas.
        /// </summary>
        private async Task<Divida> CriarDividaAsync(string nome, DateTime[] vencimentos, int pagas = 0,
            bool ehDivida = true, bool ativa = true, int? idUsuario = null, int diaVencimento = 10)
        {
            var divida = new Divida
            {
                IdUsuario = idUsuario ?? _usuario.Id,
                IdConta = _conta.Id,
                IdCategoria = _categoria.Id,
                Nome = nome,
                DiaVencimento = diaVencimento,
                DataPrimeiroVencimento = vencimentos[0],
                NumeroParcelas = vencimentos.Length,
                Valor = 100m * vencimentos.Length,
                EhDivida = ehDivida,
                Ativo = ativa
            };
            divida.Parcelas = vencimentos.Select((v, i) => new Parcela
            {
                Divida = divida,
                IdConta = _conta.Id,
                Descricao = $"{nome} {i + 1}/{vencimentos.Length}",
                Valor = 100m,
                DataVencimento = v,
                DataPagamento = i < pagas ? v : null
            }).ToList();
            await SemearAsync(divida);
            return divida;
        }

        private static DateTime[] Meses(int ano, int mes, int quantidade, int dia = 10) =>
            Enumerable.Range(0, quantidade).Select(i => new DateTime(ano, mes, dia).AddMonths(i)).ToArray();

        private Task<List<ResumoDivida>> BuscarAsync(FiltroDividas? filtro = null) =>
            _useCase.ExecutarAsync(filtro ?? new FiltroDividas(), _usuario.Id);

        private static string[] Nomes(List<ResumoDivida> lista) => lista.Select(d => d.Nome).ToArray();

        [TestMethod]
        public async Task ExecutarAsync_ResumeADivida()
        {
            var divida = await CriarDividaAsync("Notebook", Meses(2026, 10, 3, dia: 15), pagas: 1, diaVencimento: 15);

            var resumo = (await BuscarAsync()).Single();

            Assert.AreEqual(divida.Id, resumo.Id);
            Assert.AreEqual("Notebook", resumo.Nome);
            Assert.AreEqual(15, resumo.DiaVencimento);
            Assert.AreEqual(3, resumo.NumeroParcelas);
            Assert.AreEqual(300m, resumo.ValorTotal);
            Assert.AreEqual(100m, resumo.ValorParcela);
            Assert.AreEqual(1, resumo.ParcelasPagas);
            Assert.AreEqual(new DateTime(2026, 11, 15), resumo.ProximoVencimento);
            Assert.IsFalse(resumo.Paga);
        }

        [TestMethod]
        public async Task ExecutarAsync_ValorDaParcelaTruncaComoNaGeracao()
        {
            await SemearAsync(new Divida
            {
                IdUsuario = _usuario.Id,
                Nome = "Curso",
                Valor = 100m,
                NumeroParcelas = 3,
                DataPrimeiroVencimento = new DateTime(2026, 10, 10)
            });

            Assert.AreEqual(33.33m, (await BuscarAsync()).Single().ValorParcela);
        }

        [TestMethod]
        public async Task ExecutarAsync_NaoRetornaCreditosInativasNemDeOutroUsuario()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            await CriarDividaAsync("Dívida", Meses(2026, 10, 1));
            await CriarDividaAsync("Crédito", Meses(2026, 10, 1), ehDivida: false);
            await CriarDividaAsync("Excluída", Meses(2026, 10, 1), ativa: false);
            await CriarDividaAsync("De outro", Meses(2026, 10, 1), idUsuario: outro.Id);

            CollectionAssert.AreEqual(new[] { "Dívida" }, Nomes(await BuscarAsync()));
        }

        [TestMethod]
        public async Task ExecutarAsync_ComEhDividaFalseRetornaSoAsReceitas()
        {
            await CriarDividaAsync("Dívida", Meses(2026, 10, 1));
            await CriarDividaAsync("Salário", Meses(2026, 10, 1), ehDivida: false);
            await CriarDividaAsync("Excluída", Meses(2026, 10, 1), ehDivida: false, ativa: false);

            CollectionAssert.AreEqual(new[] { "Salário" }, Nomes(await BuscarAsync(new FiltroDividas(EhDivida: false))));
        }

        [TestMethod]
        public async Task ExecutarAsync_OrdenaPeloProximoVencimentoEmAbertoComAsPagasNoFim()
        {
            await CriarDividaAsync("Paga", Meses(2026, 1, 2), pagas: 2);
            await CriarDividaAsync("Dezembro", Meses(2026, 12, 1));
            // A 1ª parcela (setembro) está paga: o próximo vencimento é outubro.
            await CriarDividaAsync("Outubro", Meses(2026, 9, 3), pagas: 1);
            await CriarDividaAsync("Novembro", Meses(2026, 11, 2));

            CollectionAssert.AreEqual(new[] { "Outubro", "Novembro", "Dezembro", "Paga" }, Nomes(await BuscarAsync()));
        }

        [TestMethod]
        public async Task ExecutarAsync_EmpateNoVencimentoOrdenaPorNome()
        {
            await CriarDividaAsync("bicicleta", Meses(2026, 10, 1));
            await CriarDividaAsync("Academia", Meses(2026, 10, 1));

            CollectionAssert.AreEqual(new[] { "Academia", "bicicleta" }, Nomes(await BuscarAsync()));
        }

        [TestMethod]
        public async Task ExecutarAsync_DividaPagaQuandoTodasAsParcelasEstaoPagas()
        {
            await CriarDividaAsync("Paga", Meses(2026, 1, 2), pagas: 2);

            var resumo = (await BuscarAsync()).Single();

            Assert.IsTrue(resumo.Paga);
            Assert.IsNull(resumo.ProximoVencimento);
            Assert.AreEqual(2, resumo.ParcelasPagas);
        }

        [TestMethod]
        public async Task ExecutarAsync_ParcelaInativaNaoContaNaSituacao()
        {
            var divida = await CriarDividaAsync("Celular", Meses(2026, 10, 2), pagas: 1);
            await using (var db = CriarContexto())
            {
                db.Parcelas.Single(p => p.IdDivida == divida.Id && p.DataPagamento == null).Ativo = false;
                await db.SaveChangesAsync();
            }

            Assert.IsTrue((await BuscarAsync()).Single().Paga);
        }

        [TestMethod]
        public async Task ExecutarAsync_OcultarPagasRemoveAsPagas()
        {
            await CriarDividaAsync("Paga", Meses(2026, 1, 1), pagas: 1);
            await CriarDividaAsync("Em aberto", Meses(2026, 10, 2), pagas: 1);

            CollectionAssert.AreEqual(new[] { "Em aberto" }, Nomes(await BuscarAsync(new FiltroDividas(OcultarPagas: true))));
        }

        [TestMethod]
        [DataRow("note", DisplayName = "Parte do nome")]
        [DataRow("NOTE", DisplayName = "Maiúsculas")]
        [DataRow("  note  ", DisplayName = "Espaços nas pontas")]
        [DataRow("cartão", DisplayName = "Com acento")]
        [DataRow("cartao", DisplayName = "Sem acento")]
        public async Task ExecutarAsync_FiltraPorParteDoNome(string nome)
        {
            await CriarDividaAsync("Notebook no cartão", Meses(2026, 10, 1));
            await CriarDividaAsync("Geladeira", Meses(2026, 10, 1));

            CollectionAssert.AreEqual(new[] { "Notebook no cartão" }, Nomes(await BuscarAsync(new FiltroDividas(nome))));
        }

        [TestMethod]
        public async Task ExecutarAsync_NomeVazioNaoFiltra()
        {
            await CriarDividaAsync("Notebook", Meses(2026, 10, 1));
            await CriarDividaAsync("Geladeira", Meses(2026, 11, 1));

            Assert.HasCount(2, await BuscarAsync(new FiltroDividas("   ")));
        }

        [TestMethod]
        public async Task ExecutarAsync_FiltraDividasComParcelaVencendoNoMes()
        {
            await CriarDividaAsync("Termina em setembro", Meses(2026, 8, 2));
            await CriarDividaAsync("Passa por outubro", Meses(2026, 9, 3));
            await CriarDividaAsync("Começa em novembro", Meses(2026, 11, 2));
            // Paga também aparece, se não pedir para ocultar.
            await CriarDividaAsync("Paga em outubro", [new DateTime(2026, 10, 31)], pagas: 1);

            var lista = await BuscarAsync(new FiltroDividas(MesVencimento: new DateTime(2026, 10, 20)));

            CollectionAssert.AreEqual(new[] { "Passa por outubro", "Paga em outubro" }, Nomes(lista));
        }

        [TestMethod]
        public async Task ExecutarAsync_CombinaOsFiltros()
        {
            await CriarDividaAsync("Cartão Nubank", Meses(2026, 10, 2));
            await CriarDividaAsync("Cartão Inter", Meses(2026, 10, 1), pagas: 1);
            await CriarDividaAsync("Cartão Itaú", Meses(2026, 11, 1));
            await CriarDividaAsync("Aluguel", Meses(2026, 10, 1));

            var lista = await BuscarAsync(new FiltroDividas("cartão", new DateTime(2026, 10, 1), OcultarPagas: true));

            CollectionAssert.AreEqual(new[] { "Cartão Nubank" }, Nomes(lista));
        }
    }
}

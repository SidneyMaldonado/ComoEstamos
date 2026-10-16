using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using ComoEstamos.Test.RepositoryTests;

namespace ComoEstamos.Test.UseCaseTests
{
    [TestClass]
    public sealed class BuscarResumoMesUseCaseTests : RepositoryTestBase
    {
        private static readonly DateTime Outubro = new(2026, 10, 1);

        private BuscarResumoMesUseCase _useCase = null!;
        private Usuario _usuario = null!;
        private Conta _conta = null!;
        private Categoria _categoria = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _useCase = new BuscarResumoMesUseCase(new UnitOfWorkFactory(ContextFactory));
            _usuario = await CriarUsuarioAsync();
            _conta = new Conta { IdUsuario = _usuario.Id, Nome = "Corrente" };
            _categoria = new Categoria { IdUsuario = _usuario.Id, Nome = "Casa" };
            await SemearAsync(_conta, _categoria);
        }

        /// <summary>Dívida (ou crédito) com uma parcela para cada vencimento, de R$ <paramref name="valor"/>.</summary>
        private async Task<Divida> CriarAsync(string nome, decimal valor, bool ehDivida = true, int? idUsuario = null,
            bool ativa = true, params DateTime[] vencimentos)
        {
            var divida = new Divida
            {
                IdUsuario = idUsuario ?? _usuario.Id,
                IdConta = _conta.Id,
                IdCategoria = _categoria.Id,
                Nome = nome,
                EhDivida = ehDivida,
                Ativo = ativa,
                NumeroParcelas = vencimentos.Length,
                Valor = valor * vencimentos.Length,
                DataPrimeiroVencimento = vencimentos.Min()
            };
            divida.Parcelas = vencimentos.Select((v, i) => new Parcela
            {
                Divida = divida,
                IdConta = _conta.Id,
                Descricao = $"{nome} {i + 1}/{vencimentos.Length}",
                Valor = valor,
                DataVencimento = v
            }).ToList();
            await SemearAsync(divida);
            return divida;
        }

        [TestMethod]
        public async Task ExecutarAsync_SeparaDespesasEReceitasComTotaisEDiferenca()
        {
            await CriarAsync("Energisa", 660m, vencimentos: [new DateTime(2026, 10, 15)]);
            await CriarAsync("Funlec", 1584m, vencimentos: [new DateTime(2026, 10, 5)]);
            await CriarAsync("Salário", 7654m, ehDivida: false, vencimentos: [new DateTime(2026, 10, 5)]);

            var resumo = await _useCase.ExecutarAsync(new DateTime(2026, 10, 20), _usuario.Id);

            Assert.AreEqual(Outubro, resumo.Mes, "O dia é ignorado.");
            CollectionAssert.AreEqual(new[] { "Funlec", "Energisa" }, resumo.Despesas.Select(i => i.Nome).ToList(), "Ordem pelo vencimento.");
            Assert.AreEqual("Salário", resumo.Receitas.Single().Nome);
            Assert.AreEqual(2244m, resumo.TotalDespesas);
            Assert.AreEqual(7654m, resumo.TotalReceitas);
            Assert.AreEqual(5410m, resumo.Diferenca);
        }

        [TestMethod]
        public async Task ExecutarAsync_SoParcelasDoMesSomandoAsDaMesmaDivida()
        {
            await CriarAsync("Cartão", 100m, vencimentos: [new DateTime(2026, 9, 30), new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), new DateTime(2026, 11, 1)]);
            await CriarAsync("Placa Solar", 500m, vencimentos: [new DateTime(2026, 12, 10)]);

            var resumo = await _useCase.ExecutarAsync(Outubro, _usuario.Id);

            var item = resumo.Despesas.Single();
            Assert.AreEqual("Cartão", item.Nome, "Dívida sem parcela no mês não aparece.");
            Assert.AreEqual(200m, item.Valor);
            Assert.AreEqual(new DateTime(2026, 10, 1), item.Vencimento);
        }

        [TestMethod]
        public async Task ExecutarAsync_PagaSoQuandoTodasAsParcelasDoMesEstaoPagas()
        {
            var cartao = await CriarAsync("Cartão", 100m, vencimentos: [new DateTime(2026, 10, 5), new DateTime(2026, 10, 20)]);
            var agua = await CriarAsync("Água", 80m, vencimentos: [new DateTime(2026, 10, 10)]);
            await using (var db = CriarContexto())
            {
                db.Parcelas.Single(p => p.IdDivida == cartao.Id && p.DataVencimento.Day == 5).DataPagamento = new DateTime(2026, 10, 4);
                db.Parcelas.Single(p => p.IdDivida == agua.Id).DataPagamento = new DateTime(2026, 10, 9);
                await db.SaveChangesAsync();
            }

            var resumo = await _useCase.ExecutarAsync(Outubro, _usuario.Id);

            var itemCartao = resumo.Despesas.Single(i => i.IdDivida == cartao.Id);
            Assert.IsFalse(itemCartao.Paga);
            Assert.IsNull(itemCartao.DataPagamento);
            var itemAgua = resumo.Despesas.Single(i => i.IdDivida == agua.Id);
            Assert.IsTrue(itemAgua.Paga);
            Assert.AreEqual(new DateTime(2026, 10, 9), itemAgua.DataPagamento);
            Assert.AreEqual(280m, resumo.TotalDespesas, "O total soma pagas e em aberto.");
        }

        [TestMethod]
        public async Task ExecutarAsync_IgnoraExcluidasEDeOutroUsuario()
        {
            var outro = await CriarUsuarioAsync("outro@teste.com");
            await CriarAsync("De outro", 100m, idUsuario: outro.Id, vencimentos: [new DateTime(2026, 10, 10)]);
            await CriarAsync("Excluída", 100m, ativa: false, vencimentos: [new DateTime(2026, 10, 10)]);
            var comParcelaExcluida = await CriarAsync("Parcela excluída", 100m, vencimentos: [new DateTime(2026, 10, 10)]);
            await using (var db = CriarContexto())
            {
                db.Parcelas.Single(p => p.IdDivida == comParcelaExcluida.Id).Ativo = false;
                await db.SaveChangesAsync();
            }

            var resumo = await _useCase.ExecutarAsync(Outubro, _usuario.Id);

            Assert.IsEmpty(resumo.Despesas);
            Assert.IsEmpty(resumo.Receitas);
            Assert.AreEqual(0m, resumo.Diferenca);
        }

        [TestMethod]
        public async Task ExecutarAsync_DiferencaNegativaQuandoAsDespesasSaoMaiores()
        {
            await CriarAsync("Financiamento", 1500m, vencimentos: [new DateTime(2026, 10, 10)]);
            await CriarAsync("Aluguel recebido", 660m, ehDivida: false, vencimentos: [new DateTime(2026, 10, 10)]);

            var resumo = await _useCase.ExecutarAsync(Outubro, _usuario.Id);

            Assert.AreEqual(-840m, resumo.Diferenca);
        }
    }
}

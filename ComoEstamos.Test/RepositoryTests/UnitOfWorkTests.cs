using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.RepositoryTests
{
    [TestClass]
    public sealed class UnitOfWorkTests : RepositoryTestBase
    {
        private UnitOfWorkFactory _factory = null!;

        [TestInitialize]
        public void Inicializar() => _factory = new UnitOfWorkFactory(ContextFactory);

        [TestMethod]
        public async Task InserirAsync_SoGravaAoConfirmar()
        {
            var usuario = await CriarUsuarioAsync();
            await using var uow = await _factory.IniciarAsync();

            await uow.Carteiras.InserirAsync(new Carteira { IdUsuario = usuario.Id, Nome = "Ações" });

            await using (var db = CriarContexto())
                Assert.AreEqual(0, await db.Carteiras.CountAsync());

            await uow.ConfirmarAsync();

            await using (var db = CriarContexto())
                Assert.AreEqual(1, await db.Carteiras.CountAsync());
        }

        [TestMethod]
        public async Task DisposeAsync_SemConfirmarDescartaAlteracoes()
        {
            var usuario = await CriarUsuarioAsync();
            var conta = new Conta { IdUsuario = usuario.Id, Nome = "Corrente", Saldo = 100m };
            await SemearAsync(conta);

            await using (var uow = await _factory.IniciarAsync())
            {
                await uow.Contas.AtualizarSaldoAsync(conta.Id, 0m);
                await uow.Carteiras.InserirAsync(new Carteira { IdUsuario = usuario.Id, Nome = "Ações" });
            }

            await using var db = CriarContexto();
            Assert.AreEqual(100m, (await db.Contas.SingleAsync()).Saldo);
            Assert.AreEqual(0, await db.Carteiras.CountAsync());
        }

        [TestMethod]
        public async Task ConfirmarAsync_GravaAlteracoesDeVariosRepositoriosJuntas()
        {
            var usuario = await CriarUsuarioAsync();
            var conta = new Conta { IdUsuario = usuario.Id, Nome = "Corrente", Saldo = 100m };
            var categoria = new Categoria { IdUsuario = usuario.Id, Nome = "Moradia" };
            await SemearAsync(conta, categoria);
            var divida = new Divida { IdUsuario = usuario.Id, Nome = "Aluguel", Valor = 50m };
            await SemearAsync(divida);
            var parcela = new Parcela { IdDivida = divida.Id, IdConta = conta.Id, IdCategoria = categoria.Id, Descricao = "1/1", Valor = 50m };
            await SemearAsync(parcela);

            await using (var uow = await _factory.IniciarAsync())
            {
                await uow.Parcelas.RegistrarPagamentoAsync(parcela.Id, new DateTime(2026, 9, 30));
                await uow.Contas.AtualizarSaldoAsync(conta.Id, 50m);
                await uow.Categorias.DesativarAsync(categoria.Id);
                await uow.ConfirmarAsync();
            }

            await using var db = CriarContexto();
            Assert.AreEqual(new DateTime(2026, 9, 30), (await db.Parcelas.SingleAsync()).DataPagamento);
            Assert.AreEqual(50m, (await db.Contas.SingleAsync()).Saldo);
            Assert.IsFalse((await db.Categorias.SingleAsync()).Ativo);
        }

        [TestMethod]
        public async Task ObterPorIdAsync_EnxergaAlteracoesPendentes()
        {
            var usuario = await CriarUsuarioAsync();
            var conta = new Conta { IdUsuario = usuario.Id, Nome = "Corrente", Saldo = 100m };
            await SemearAsync(conta);
            await using var uow = await _factory.IniciarAsync();

            await uow.Contas.AtualizarSaldoAsync(conta.Id, 70m);
            var obtida = await uow.Contas.ObterPorIdAsync(conta.Id);

            Assert.AreEqual(70m, obtida?.Saldo);
        }

        [TestMethod]
        public async Task ObterPorIdAsync_RetornaAMesmaInstanciaRastreada()
        {
            var usuario = await CriarUsuarioAsync();
            var conta = new Conta { IdUsuario = usuario.Id, Nome = "Corrente", Saldo = 100m };
            await SemearAsync(conta);
            await using var uow = await _factory.IniciarAsync();

            var obtida = await uow.Contas.ObterPorIdAsync(conta.Id);
            obtida!.Nome = "Conta Principal";
            await uow.ConfirmarAsync();

            await using var db = CriarContexto();
            Assert.AreEqual("Conta Principal", (await db.Contas.SingleAsync()).Nome);
        }

        [TestMethod]
        public async Task ListarAsync_LeSomenteOQueJaFoiGravado()
        {
            var usuario = await CriarUsuarioAsync();
            await using var uow = await _factory.IniciarAsync();

            await uow.Carteiras.InserirAsync(new Carteira { IdUsuario = usuario.Id, Nome = "Ações" });

            Assert.IsEmpty(await uow.Carteiras.ListarPorUsuarioAsync(usuario.Id));
            await uow.ConfirmarAsync();
            Assert.HasCount(1, await uow.Carteiras.ListarPorUsuarioAsync(usuario.Id));
        }

        [TestMethod]
        public async Task InserirAsync_RelacionaEntidadesNovasPelaNavegacao()
        {
            var usuario = await CriarUsuarioAsync();
            var carteira = new Carteira { IdUsuario = usuario.Id, Nome = "Ações" };
            await using (var uow = await _factory.IniciarAsync())
            {
                await uow.Carteiras.InserirAsync(carteira);
                await uow.Investimentos.InserirAsync(new Investimento { Carteira = carteira, Nome = "PETR4" });
                await uow.ConfirmarAsync();
            }

            await using var db = CriarContexto();
            var investimento = await db.Investimentos.SingleAsync();
            Assert.AreEqual(carteira.Id, investimento.IdCarteira);
        }

        [TestMethod]
        public async Task ConfirmarAsync_PreencheAuditoria()
        {
            var usuario = await CriarUsuarioAsync();
            var conta = new Conta { IdUsuario = usuario.Id, Nome = "Corrente" };
            await SemearAsync(conta);

            await using (var uow = await _factory.IniciarAsync())
            {
                await uow.Contas.AtualizarSaldoAsync(conta.Id, 10m);
                await uow.ConfirmarAsync();
            }

            await using var db = CriarContexto();
            Assert.IsNotNull((await db.Contas.SingleAsync()).DataAlteracao);
        }
    }
}

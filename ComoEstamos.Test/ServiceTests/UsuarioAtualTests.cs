using ComoEstamos.Core.Services;
using ComoEstamos.Data.Repository;
using ComoEstamos.Test.RepositoryTests;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Test.ServiceTests
{
    [TestClass]
    public sealed class UsuarioAtualTests : RepositoryTestBase
    {
        [TestMethod]
        public async Task ObterIdAsync_CriaOUsuarioLocalNaPrimeiraChamadaEReaproveita()
        {
            var usuarioAtual = new UsuarioAtual(new UsuarioRepository(ContextFactory));

            var ids = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => usuarioAtual.ObterIdAsync()));

            await using var db = CriarContexto();
            var usuario = await db.Usuarios.SingleAsync();
            Assert.AreEqual("local@comoestamos.app", usuario.Email);
            Assert.IsTrue(ids.All(id => id == usuario.Id));
        }

        [TestMethod]
        public async Task ObterIdAsync_UsaOUsuarioLocalJaExistente()
        {
            var existente = await CriarUsuarioAsync("local@comoestamos.app");

            var id = await new UsuarioAtual(new UsuarioRepository(ContextFactory)).ObterIdAsync();

            Assert.AreEqual(existente.Id, id);
        }
    }
}

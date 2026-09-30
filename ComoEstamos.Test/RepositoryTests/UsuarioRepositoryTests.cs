using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;

namespace ComoEstamos.Test.RepositoryTests
{
    [TestClass]
    public sealed class UsuarioRepositoryTests : RepositoryTestBase
    {
        private UsuarioRepository _repository = null!;

        [TestInitialize]
        public void Inicializar() => _repository = new UsuarioRepository(ContextFactory);

        private Task<Usuario> CriarAsync(string email = "joao@teste.com", string senha = "Senha@123") =>
            _repository.CriarAsync(new Usuario { Nome = "João", Email = email }, senha);

        [TestMethod]
        public async Task CriarAsync_PersisteUsuarioComEmailNormalizado()
        {
            var usuario = await CriarAsync("  Joao@Teste.COM ");

            var obtido = await _repository.ObterPorIdAsync(usuario.Id);
            Assert.IsNotNull(obtido);
            Assert.AreEqual("joao@teste.com", obtido.Email);
            Assert.AreEqual("João", obtido.Nome);
        }

        [TestMethod]
        public async Task CriarAsync_GuardaHashENaoASenha()
        {
            var usuario = await CriarAsync(senha: "Senha@123");

            var obtido = await _repository.ObterPorIdAsync(usuario.Id);
            Assert.IsNotNull(obtido);
            Assert.AreNotEqual("Senha@123", obtido.SenhaHash);
            Assert.DoesNotContain("Senha@123", obtido.SenhaHash);
            Assert.HasCount(3, obtido.SenhaHash.Split('.'));
        }

        [TestMethod]
        public async Task CriarAsync_GeraSaltDiferenteParaSenhasIguais()
        {
            var primeiro = await CriarAsync("a@teste.com", "MesmaSenha");
            var segundo = await CriarAsync("b@teste.com", "MesmaSenha");

            Assert.AreNotEqual(primeiro.SenhaHash, segundo.SenhaHash);
        }

        [TestMethod]
        public async Task ObterPorEmailAsync_EncontraIgnorandoMaiusculasEEspacos()
        {
            var usuario = await CriarAsync("joao@teste.com");

            var obtido = await _repository.ObterPorEmailAsync("  JOAO@teste.com ");

            Assert.IsNotNull(obtido);
            Assert.AreEqual(usuario.Id, obtido.Id);
        }

        [TestMethod]
        public async Task ObterPorEmailAsync_RetornaNuloQuandoNaoExiste()
        {
            await CriarAsync("joao@teste.com");

            var obtido = await _repository.ObterPorEmailAsync("maria@teste.com");

            Assert.IsNull(obtido);
        }

        [TestMethod]
        public async Task AutenticarAsync_RetornaUsuarioComSenhaCorreta()
        {
            var usuario = await CriarAsync("joao@teste.com", "Senha@123");

            var autenticado = await _repository.AutenticarAsync("Joao@Teste.com", "Senha@123");

            Assert.IsNotNull(autenticado);
            Assert.AreEqual(usuario.Id, autenticado.Id);
        }

        [TestMethod]
        public async Task AutenticarAsync_RetornaNuloComSenhaErrada()
        {
            await CriarAsync("joao@teste.com", "Senha@123");

            var autenticado = await _repository.AutenticarAsync("joao@teste.com", "senha@123");

            Assert.IsNull(autenticado);
        }

        [TestMethod]
        public async Task AutenticarAsync_RetornaNuloParaEmailInexistente()
        {
            var autenticado = await _repository.AutenticarAsync("ninguem@teste.com", "Senha@123");

            Assert.IsNull(autenticado);
        }

        [TestMethod]
        public async Task AutenticarAsync_RetornaNuloParaUsuarioInativo()
        {
            var usuario = await CriarAsync("joao@teste.com", "Senha@123");
            await _repository.DesativarAsync(usuario.Id);

            var autenticado = await _repository.AutenticarAsync("joao@teste.com", "Senha@123");

            Assert.IsNull(autenticado);
        }

        [TestMethod]
        public async Task AutenticarAsync_RetornaNuloParaHashEmFormatoInvalido()
        {
            await SemearAsync(new Usuario { Nome = "Legado", Email = "legado@teste.com", SenhaHash = "hash-invalido" });

            var autenticado = await _repository.AutenticarAsync("legado@teste.com", "qualquer");

            Assert.IsNull(autenticado);
        }

        [TestMethod]
        public async Task AlterarSenhaAsync_PassaAAutenticarComANovaSenha()
        {
            var usuario = await CriarAsync("joao@teste.com", "SenhaAntiga");

            await _repository.AlterarSenhaAsync(usuario.Id, "SenhaNova");

            Assert.IsNull(await _repository.AutenticarAsync("joao@teste.com", "SenhaAntiga"));
            Assert.IsNotNull(await _repository.AutenticarAsync("joao@teste.com", "SenhaNova"));
        }

        [TestMethod]
        public async Task AlterarSenhaAsync_PreencheDataAlteracao()
        {
            var usuario = await CriarAsync();

            await _repository.AlterarSenhaAsync(usuario.Id, "SenhaNova");

            var obtido = await _repository.ObterPorIdAsync(usuario.Id);
            Assert.IsNotNull(obtido?.DataAlteracao);
        }

        [TestMethod]
        public async Task AlterarSenhaAsync_LancaExcecaoQuandoUsuarioNaoExiste()
        {
            await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => _repository.AlterarSenhaAsync(999, "SenhaNova"));
        }

        [TestMethod]
        public async Task AtualizarAsync_AlteraNomeSemPerderASenha()
        {
            var usuario = await CriarAsync("joao@teste.com", "Senha@123");

            usuario.Nome = "João Silva";
            await _repository.AtualizarAsync(usuario);

            var obtido = await _repository.ObterPorIdAsync(usuario.Id);
            Assert.AreEqual("João Silva", obtido?.Nome);
            Assert.IsNotNull(await _repository.AutenticarAsync("joao@teste.com", "Senha@123"));
        }
    }
}

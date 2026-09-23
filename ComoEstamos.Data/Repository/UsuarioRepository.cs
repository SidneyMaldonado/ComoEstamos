using System.Security.Cryptography;
using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IUsuarioRepository : IRepository<Usuario>
    {
        Task<Usuario?> ObterPorEmailAsync(string email);
        Task<Usuario> CriarAsync(Usuario usuario, string senha);
        Task<Usuario?> AutenticarAsync(string email, string senha);
        Task AlterarSenhaAsync(int idUsuario, string novaSenha);
    }

    public class UsuarioRepository(IDbContextFactory<AppDbContext> contextFactory)
        : RepositoryBase<Usuario>(contextFactory), IUsuarioRepository
    {
        private const int Iteracoes = 100_000;
        private const int TamanhoSalt = 16;
        private const int TamanhoHash = 32;

        public async Task<Usuario?> ObterPorEmailAsync(string email)
        {
            var emailNormalizado = NormalizarEmail(email);
            await using var db = await ContextFactory.CreateDbContextAsync();
            return await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == emailNormalizado);
        }

        public Task<Usuario> CriarAsync(Usuario usuario, string senha)
        {
            usuario.Email = NormalizarEmail(usuario.Email);
            usuario.SenhaHash = GerarHash(senha);
            return InserirAsync(usuario);
        }

        public async Task<Usuario?> AutenticarAsync(string email, string senha)
        {
            var usuario = await ObterPorEmailAsync(email);
            if (usuario is null || !usuario.Ativo || !VerificarHash(senha, usuario.SenhaHash))
                return null;
            return usuario;
        }

        public async Task AlterarSenhaAsync(int idUsuario, string novaSenha)
        {
            await using var db = await ContextFactory.CreateDbContextAsync();
            var usuario = await db.Usuarios.FindAsync(idUsuario)
                ?? throw new KeyNotFoundException($"Usuario {idUsuario} não encontrado.");
            usuario.SenhaHash = GerarHash(novaSenha);
            await db.SaveChangesAsync();
        }

        private static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();

        // Formato: iteracoes.salt.hash (salt e hash em Base64)
        private static string GerarHash(string senha)
        {
            var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
            var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
            return $"{Iteracoes}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        private static bool VerificarHash(string senha, string senhaHash)
        {
            var partes = senhaHash.Split('.');
            if (partes.Length != 3 || !int.TryParse(partes[0], out var iteracoes))
                return false;

            var salt = Convert.FromBase64String(partes[1]);
            var hashEsperado = Convert.FromBase64String(partes[2]);
            var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, hashEsperado.Length);
            return CryptographicOperations.FixedTimeEquals(hash, hashEsperado);
        }
    }
}

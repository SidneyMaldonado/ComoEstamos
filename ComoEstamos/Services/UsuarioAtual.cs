using System.Security.Cryptography;
using ComoEstamos.Data.Model;
using ComoEstamos.Data.Repository;

namespace ComoEstamos.Services
{
    /// <summary>
    /// Usuário dono dos dados exibidos nas telas. Enquanto não há login, é um usuário
    /// local padrão, criado na primeira vez que o app precisa dele.
    /// </summary>
    public interface IUsuarioAtual
    {
        Task<int> ObterIdAsync();
    }

    public class UsuarioAtual(IUsuarioRepository usuarioRepository) : IUsuarioAtual
    {
        private const string EmailUsuarioLocal = "local@comoestamos.app";

        private readonly SemaphoreSlim trava = new(1, 1);
        private int? id;

        public async Task<int> ObterIdAsync()
        {
            if (id is int idEmCache)
                return idEmCache;

            await trava.WaitAsync();
            try
            {
                if (id is null)
                {
                    var usuario = await usuarioRepository.ObterPorEmailAsync(EmailUsuarioLocal)
                        ?? await usuarioRepository.CriarAsync(
                            new Usuario { Nome = "Usuário local", Email = EmailUsuarioLocal },
                            // Ninguém faz login com este usuário; a senha só cumpre a obrigatoriedade.
                            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
                    id = usuario.Id;
                }
                return id.Value;
            }
            finally
            {
                trava.Release();
            }
        }
    }
}

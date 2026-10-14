using ComoEstamos.Data;

namespace ComoEstamos.Core.UseCases.Backup
{
    /// <summary>O que a tela mostra antes de restaurar: quando e por qual versão o backup foi feito, e o que ele tem.</summary>
    public record ResumoBackup(DateTime DataHora, string VersaoApp, ContagemRegistros Contagens);

    public interface IValidarBackupUseCase
    {
        /// <summary>
        /// Confere o arquivo de backup sem mudar nada. Recusa com <see cref="RegraDeNegocioException"/> se não for
        /// um backup do app, estiver corrompido ou tiver sido feito por uma versão mais nova.
        /// </summary>
        Task<ResumoBackup> ExecutarAsync(string caminhoZip);
    }

    public class ValidarBackupUseCase(IArquivoBanco arquivoBanco) : IValidarBackupUseCase
    {
        public async Task<ResumoBackup> ExecutarAsync(string caminhoZip)
        {
            var temporaria = PacoteBackup.CriarPastaTemporaria();
            try
            {
                var (manifesto, _) = await PacoteBackup.AbrirAsync(arquivoBanco, caminhoZip, temporaria);
                return new ResumoBackup(manifesto.DataHora, manifesto.VersaoApp, manifesto.Contagens);
            }
            finally
            {
                PacoteBackup.ApagarPasta(temporaria);
            }
        }
    }
}

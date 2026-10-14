using ComoEstamos.Data;

namespace ComoEstamos.Core.UseCases.Backup
{
    public interface IGerarBackupUseCase
    {
        /// <summary>
        /// Gera o backup do banco inteiro em <paramref name="pastaDestino"/>
        /// (<c>como-estamos-backup-AAAA-MM-DD-HHmm.zip</c>) e devolve o caminho do arquivo.
        /// </summary>
        Task<string> ExecutarAsync(string pastaDestino, string versaoApp);
    }

    public class GerarBackupUseCase(IArquivoBanco arquivoBanco) : IGerarBackupUseCase
    {
        public async Task<string> ExecutarAsync(string pastaDestino, string versaoApp)
        {
            Directory.CreateDirectory(pastaDestino);
            var caminhoZip = Path.Combine(pastaDestino, $"como-estamos-backup-{DateTime.Now:yyyy-MM-dd-HHmm}.zip");

            var temporaria = PacoteBackup.CriarPastaTemporaria();
            try
            {
                await PacoteBackup.GerarAsync(arquivoBanco, caminhoZip, versaoApp, temporaria);
                return caminhoZip;
            }
            finally
            {
                PacoteBackup.ApagarPasta(temporaria);
            }
        }
    }
}

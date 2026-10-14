using ComoEstamos.Data;

namespace ComoEstamos.Core.UseCases.Backup
{
    public interface IRestaurarBackupUseCase
    {
        /// <summary>
        /// Valida o backup e substitui o banco inteiro por ele (não mistura com os dados atuais); depois aplica as
        /// migrations. Antes, grava o backup de segurança do banco atual (<see cref="RestaurarBackupUseCase.NomeBackupSeguranca"/>,
        /// na pasta do banco). Se a troca falhar, o banco atual volta.
        /// </summary>
        Task ExecutarAsync(string caminhoZip, string versaoApp);
    }

    public class RestaurarBackupUseCase(IArquivoBanco arquivoBanco) : IRestaurarBackupUseCase
    {
        /// <summary>Backup do banco de antes do último restore; cada restore substitui o anterior.</summary>
        public const string NomeBackupSeguranca = "backup-antes-do-restore.zip";

        public async Task ExecutarAsync(string caminhoZip, string versaoApp)
        {
            var temporariaBackup = PacoteBackup.CriarPastaTemporaria();
            var temporariaAtual = PacoteBackup.CriarPastaTemporaria();
            try
            {
                var (_, bancoDoBackup) = await PacoteBackup.AbrirAsync(arquivoBanco, caminhoZip, temporariaBackup);

                var caminhoSeguranca = Path.Combine(Path.GetDirectoryName(arquivoBanco.Caminho) ?? string.Empty, NomeBackupSeguranca);
                var bancoAtual = await PacoteBackup.GerarAsync(arquivoBanco, caminhoSeguranca, versaoApp, temporariaAtual);

                try
                {
                    await arquivoBanco.SubstituirPorAsync(bancoDoBackup);
                }
                catch
                {
                    await arquivoBanco.SubstituirPorAsync(bancoAtual);
                    throw;
                }
            }
            finally
            {
                PacoteBackup.ApagarPasta(temporariaBackup);
                PacoteBackup.ApagarPasta(temporariaAtual);
            }
        }
    }
}

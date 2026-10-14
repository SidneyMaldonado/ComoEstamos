using ComoEstamos.Core.UseCases.Backup;

namespace ComoEstamos.Core.Services
{
    /// <summary>Backup e restore do banco inteiro, usados pela tela Configurações.</summary>
    public interface IBackupService
    {
        /// <summary>Gera o <c>.zip</c> do backup em <paramref name="pastaDestino"/> e devolve o caminho.</summary>
        Task<string> GerarAsync(string pastaDestino, string versaoApp);

        /// <summary>Confere o arquivo sem mudar nada e devolve o resumo para a confirmação.</summary>
        Task<ResumoBackup> ValidarAsync(string caminhoZip);

        /// <summary>Substitui todos os dados atuais pelos do backup.</summary>
        Task RestaurarAsync(string caminhoZip, string versaoApp);
    }

    public class BackupService(
        IGerarBackupUseCase gerar,
        IValidarBackupUseCase validar,
        IRestaurarBackupUseCase restaurar) : IBackupService
    {
        public Task<string> GerarAsync(string pastaDestino, string versaoApp) => gerar.ExecutarAsync(pastaDestino, versaoApp);

        public Task<ResumoBackup> ValidarAsync(string caminhoZip) => validar.ExecutarAsync(caminhoZip);

        public Task RestaurarAsync(string caminhoZip, string versaoApp) => restaurar.ExecutarAsync(caminhoZip, versaoApp);
    }
}

using System.IO.Compression;
using System.Text.Json;
using ComoEstamos.Data;

namespace ComoEstamos.Core.UseCases.Backup
{
    /// <summary>Conteúdo do <c>manifest.json</c> do backup.</summary>
    /// <param name="Formato">Versão do formato do arquivo de backup (hoje, 1).</param>
    /// <param name="UltimaMigration">Id da última migration aplicada no banco copiado.</param>
    public record ManifestoBackup(int Formato, string VersaoApp, string UltimaMigration, DateTime DataHora, ContagemRegistros Contagens);

    /// <summary>
    /// O arquivo de backup: um <c>.zip</c> com a cópia do banco (<c>comoestamos.db3</c>) e o <c>manifest.json</c>.
    /// Comum aos casos de uso de gerar, validar e restaurar.
    /// </summary>
    internal static class PacoteBackup
    {
        public const int FormatoAtual = 1;
        public const string EntradaBanco = "comoestamos.db3";
        public const string EntradaManifesto = "manifest.json";

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

        /// <summary>Pasta temporária própria da operação; quem chama apaga com <see cref="ApagarPasta"/>.</summary>
        public static string CriarPastaTemporaria()
        {
            var pasta = Path.Combine(Path.GetTempPath(), $"comoestamos-backup-{Guid.NewGuid():N}");
            Directory.CreateDirectory(pasta);
            return pasta;
        }

        public static void ApagarPasta(string pasta)
        {
            try
            {
                if (Directory.Exists(pasta))
                    Directory.Delete(pasta, recursive: true);
            }
            catch (IOException)
            {
                // Arquivo temporário preso: o sistema limpa depois.
            }
        }

        /// <summary>Copia o banco em uso e grava o <c>.zip</c> em <paramref name="caminhoZip"/> (substitui se existir).</summary>
        /// <returns>O caminho da cópia do banco, dentro de <paramref name="pastaTemporaria"/>.</returns>
        public static async Task<string> GerarAsync(IArquivoBanco arquivoBanco, string caminhoZip, string versaoApp, string pastaTemporaria)
        {
            var copia = Path.Combine(pastaTemporaria, EntradaBanco);
            await arquivoBanco.CopiarParaAsync(copia);

            var manifesto = new ManifestoBackup(
                FormatoAtual,
                versaoApp,
                (await arquivoBanco.MigrationsAplicadasAsync(copia)).LastOrDefault() ?? string.Empty,
                DateTime.Now,
                await arquivoBanco.ContarAsync(copia));

            File.Delete(caminhoZip);
            await using var zip = await ZipFile.OpenAsync(caminhoZip, ZipArchiveMode.Create);
            await zip.CreateEntryFromFileAsync(copia, EntradaBanco);
            var entrada = zip.CreateEntry(EntradaManifesto);
            await using (var fluxo = await entrada.OpenAsync())
                await JsonSerializer.SerializeAsync(fluxo, manifesto, Json);
            return copia;
        }

        /// <summary>
        /// Abre o <c>.zip</c>, extrai o banco para <paramref name="pastaTemporaria"/> e confere tudo o que o restore
        /// exige. Recusa com <see cref="RegraDeNegocioException"/> (mensagem para o usuário).
        /// </summary>
        /// <returns>O manifesto e o caminho do banco extraído.</returns>
        public static async Task<(ManifestoBackup Manifesto, string CaminhoBanco)> AbrirAsync(
            IArquivoBanco arquivoBanco, string caminhoZip, string pastaTemporaria)
        {
            var caminhoBanco = Path.Combine(pastaTemporaria, EntradaBanco);
            ManifestoBackup? manifesto;
            try
            {
                await using var zip = await ZipFile.OpenReadAsync(caminhoZip);
                // Só as duas entradas conhecidas, pelo nome: nada do zip é extraído com o caminho que vem nele.
                var entradaManifesto = zip.GetEntry(EntradaManifesto);
                var entradaBanco = zip.GetEntry(EntradaBanco);
                if (entradaManifesto is null || entradaBanco is null)
                    throw new RegraDeNegocioException("O arquivo não é um backup do Como Estamos.");

                await using (var fluxo = await entradaManifesto.OpenAsync())
                    manifesto = await JsonSerializer.DeserializeAsync<ManifestoBackup>(fluxo, Json);
                await entradaBanco.ExtractToFileAsync(caminhoBanco);
            }
            catch (Exception ex) when (ex is InvalidDataException or JsonException or NotSupportedException)
            {
                throw new RegraDeNegocioException("O arquivo não é um backup do Como Estamos.");
            }

            if (manifesto is null)
                throw new RegraDeNegocioException("O arquivo não é um backup do Como Estamos.");
            if (manifesto.Formato != FormatoAtual)
                throw new RegraDeNegocioException("Este backup foi feito por outra versão do app. Atualize o app para restaurá-lo.");
            if (!await arquivoBanco.EstaIntegroAsync(caminhoBanco))
                throw new RegraDeNegocioException("O arquivo de backup está corrompido.");

            var aplicadas = await arquivoBanco.MigrationsAplicadasAsync(caminhoBanco);
            if (aplicadas.Count == 0)
                throw new RegraDeNegocioException("O arquivo não é um backup do Como Estamos.");
            if (aplicadas.Except(arquivoBanco.MigrationsConhecidas).Any())
                throw new RegraDeNegocioException("Este backup foi feito por uma versão mais nova do app. Atualize o app para restaurá-lo.");

            return (manifesto, caminhoBanco);
        }
    }
}

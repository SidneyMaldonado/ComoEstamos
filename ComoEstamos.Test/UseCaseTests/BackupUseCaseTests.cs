using System.IO.Compression;
using System.Text.Json;
using ComoEstamos.Core;
using ComoEstamos.Core.UseCases.Backup;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ComoEstamos.Test.UseCaseTests
{
    /// <summary>
    /// Gerar, validar e restaurar backup. Diferente dos outros testes, usa SQLite em <b>arquivo</b> (numa pasta
    /// temporária por teste): o backup copia e troca o arquivo do banco, o que o InMemory não tem.
    /// </summary>
    [TestClass]
    public sealed class BackupUseCaseTests
    {
        private const string VersaoApp = "1.0";

        private string _pasta = null!;
        private ArquivoBanco _arquivoBanco = null!;
        private Usuario _usuario = null!;

        [TestInitialize]
        public async Task Inicializar()
        {
            _pasta = Path.Combine(Path.GetTempPath(), $"comoestamos-teste-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_pasta);
            _arquivoBanco = new ArquivoBanco(Path.Combine(_pasta, "comoestamos.db3"));

            await using var db = Contexto();
            await db.Database.MigrateAsync();
            _usuario = new Usuario { Nome = "Usuário Teste", Email = "usuario@teste.com", SenhaHash = "hash" };
            db.Usuarios.Add(_usuario);
            await db.SaveChangesAsync();
        }

        [TestCleanup]
        public void Limpar()
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(_pasta, recursive: true);
        }

        private AppDbContext Contexto(string? caminho = null) =>
            new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={caminho ?? _arquivoBanco.Caminho}").Options);

        private async Task SemearAsync(params object[] entidades)
        {
            await using var db = Contexto();
            db.AddRange(entidades);
            await db.SaveChangesAsync();
        }

        private Conta NovaConta(string nome, bool ativa = true) => new() { IdUsuario = _usuario.Id, Nome = nome, Ativo = ativa };

        private Divida NovaDivida(string nome, bool ehDivida) =>
            new() { Nome = nome, Valor = 100m, EhDivida = ehDivida, DataPrimeiroVencimento = new DateTime(2026, 10, 10) };

        private async Task<string[]> NomesDasContasAsync()
        {
            await using var db = Contexto();
            return await db.Contas.OrderBy(c => c.Nome).Select(c => c.Nome).ToArrayAsync();
        }

        private Task<string> GerarAsync() => new GerarBackupUseCase(_arquivoBanco).ExecutarAsync(Path.Combine(_pasta, "saida"), VersaoApp);

        private Task<ResumoBackup> ValidarAsync(string caminhoZip) => new ValidarBackupUseCase(_arquivoBanco).ExecutarAsync(caminhoZip);

        private Task RestaurarAsync(string caminhoZip) => new RestaurarBackupUseCase(_arquivoBanco).ExecutarAsync(caminhoZip, VersaoApp);

        /// <summary>Monta um zip à mão: o banco do arquivo informado (opcional) e o manifesto (opcional).</summary>
        private string MontarZip(string? caminhoBanco, string? manifestoJson)
        {
            var caminhoZip = Path.Combine(_pasta, $"montado-{Guid.NewGuid():N}.zip");
            // O pool do SQLite mantém o arquivo aberto e o Windows não deixa lê-lo.
            SqliteConnection.ClearAllPools();
            using var zip = ZipFile.Open(caminhoZip, ZipArchiveMode.Create);
            if (caminhoBanco is not null)
                zip.CreateEntryFromFile(caminhoBanco, PacoteBackupNomes.Banco);
            if (manifestoJson is not null)
            {
                using var escritor = new StreamWriter(zip.CreateEntry(PacoteBackupNomes.Manifesto).Open());
                escritor.Write(manifestoJson);
            }
            return caminhoZip;
        }

        /// <summary>Manifesto válido (formato 1), para os zips montados à mão.</summary>
        private static string ManifestoJson(int formato = 1) =>
            JsonSerializer.Serialize(new ManifestoBackup(formato, VersaoApp, "x", new DateTime(2026, 10, 14, 15, 30, 0),
                new ContagemRegistros(1, 0, 0, 0, 0, 0, 0, 0)), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        /// <summary>Banco separado, migrado só até <paramref name="migration"/> (nula: todas), com uma conta "Antiga".</summary>
        private async Task<string> CriarBancoAvulsoAsync(string? migration = null)
        {
            var caminho = Path.Combine(_pasta, $"avulso-{Guid.NewGuid():N}.db3");
            await using var db = Contexto(caminho);
            await db.GetService<IMigrator>().MigrateAsync(migration);
            var usuario = new Usuario { Nome = "Outro", Email = "outro@teste.com", SenhaHash = "hash" };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
            // SQL direto: o modelo atual pode ter colunas que a migration antiga ainda não criou.
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO tb_conta (id_usuario, nm_conta, nr_saldo, dm_ativo, dt_criacao) VALUES ({0}, 'Antiga', '0', 1, '2026-10-01')", usuario.Id);
            return caminho;
        }

        // ---------- Gerar ----------

        [TestMethod]
        public async Task Gerar_CriaZipComOBancoEOManifesto()
        {
            await SemearAsync(NovaConta("Nubank"), NovaConta("Excluída", ativa: false),
                NovaDivida("Notebook", ehDivida: true), NovaDivida("Salário", ehDivida: false));

            var caminhoZip = await GerarAsync();

            StringAssert.StartsWith(Path.GetFileName(caminhoZip), "como-estamos-backup-");
            using var zip = ZipFile.OpenRead(caminhoZip);
            Assert.IsNotNull(zip.GetEntry(PacoteBackupNomes.Banco));
            using var leitor = new StreamReader(zip.GetEntry(PacoteBackupNomes.Manifesto)!.Open());
            var manifesto = JsonSerializer.Deserialize<ManifestoBackup>(leitor.ReadToEnd(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            Assert.AreEqual(1, manifesto.Formato);
            Assert.AreEqual(VersaoApp, manifesto.VersaoApp);
            Assert.AreEqual(_arquivoBanco.MigrationsConhecidas[^1], manifesto.UltimaMigration);
            // Só os ativos; dívidas e receitas separadas.
            Assert.AreEqual(new ContagemRegistros(1, 1, 1, 0, 0, 0, 0, 0), manifesto.Contagens);
        }

        // ---------- Validar ----------

        [TestMethod]
        public async Task Validar_DevolveOResumoDoManifesto()
        {
            await SemearAsync(NovaConta("Nubank"));
            var caminhoZip = await GerarAsync();

            var resumo = await ValidarAsync(caminhoZip);

            Assert.AreEqual(VersaoApp, resumo.VersaoApp);
            Assert.AreEqual(1, resumo.Contagens.Contas);
        }

        [TestMethod]
        public async Task Validar_RecusaArquivoQueNaoEZip()
        {
            var caminho = Path.Combine(_pasta, "texto.zip");
            await File.WriteAllTextAsync(caminho, "não sou um zip");

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => ValidarAsync(caminho));
        }

        [TestMethod]
        public async Task Validar_RecusaZipSemManifesto()
        {
            var caminhoZip = MontarZip(_arquivoBanco.Caminho, manifestoJson: null);

            var ex = await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => ValidarAsync(caminhoZip));
            Assert.AreEqual("O arquivo não é um backup do Como Estamos.", ex.Message);
        }

        [TestMethod]
        public async Task Validar_RecusaFormatoDesconhecido()
        {
            var caminhoZip = MontarZip(await CriarBancoAvulsoAsync(), ManifestoJson(formato: 2));

            var ex = await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => ValidarAsync(caminhoZip));
            StringAssert.Contains(ex.Message, "Atualize o app");
        }

        [TestMethod]
        public async Task Validar_RecusaBancoCorrompido()
        {
            var lixo = Path.Combine(_pasta, "lixo.db3");
            await File.WriteAllTextAsync(lixo, new string('x', 4096));
            var caminhoZip = MontarZip(lixo, ManifestoJson());

            var ex = await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => ValidarAsync(caminhoZip));
            Assert.AreEqual("O arquivo de backup está corrompido.", ex.Message);
        }

        [TestMethod]
        public async Task Validar_RecusaBancoDeVersaoMaisNova()
        {
            var caminhoBanco = await CriarBancoAvulsoAsync();
            await using (var db = Contexto(caminhoBanco))
                await db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('99991231000000_Futura', '10.0.0')");
            var caminhoZip = MontarZip(caminhoBanco, ManifestoJson());

            var ex = await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => ValidarAsync(caminhoZip));
            StringAssert.Contains(ex.Message, "versão mais nova");
        }

        // ---------- Restaurar ----------

        [TestMethod]
        public async Task Restaurar_SubstituiTodosOsDadosEGuardaOBackupDeSeguranca()
        {
            await SemearAsync(NovaConta("Do backup"));
            var caminhoZip = await GerarAsync();
            await SemearAsync(NovaConta("Depois do backup"));

            await RestaurarAsync(caminhoZip);

            CollectionAssert.AreEqual(new[] { "Do backup" }, await NomesDasContasAsync());
            // O backup de segurança tem os dados de antes do restore.
            var seguranca = Path.Combine(_pasta, RestaurarBackupUseCase.NomeBackupSeguranca);
            Assert.AreEqual(2, (await ValidarAsync(seguranca)).Contagens.Contas);
        }

        [TestMethod]
        public async Task Restaurar_RecusadoNaoMudaNada()
        {
            await SemearAsync(NovaConta("Atual"));
            var caminhoZip = MontarZip(_arquivoBanco.Caminho, manifestoJson: null);

            await Assert.ThrowsExactlyAsync<RegraDeNegocioException>(() => RestaurarAsync(caminhoZip));

            CollectionAssert.AreEqual(new[] { "Atual" }, await NomesDasContasAsync());
            Assert.IsFalse(File.Exists(Path.Combine(_pasta, RestaurarBackupUseCase.NomeBackupSeguranca)));
        }

        [TestMethod]
        public async Task Restaurar_BackupDeVersaoAnteriorEhMigrado()
        {
            var conhecidas = _arquivoBanco.MigrationsConhecidas;
            var caminhoZip = MontarZip(await CriarBancoAvulsoAsync(conhecidas[^2]), ManifestoJson());

            await RestaurarAsync(caminhoZip);

            CollectionAssert.AreEqual(new[] { "Antiga" }, await NomesDasContasAsync());
            await using var db = Contexto();
            CollectionAssert.AreEqual(conhecidas.ToArray(), (await db.Database.GetAppliedMigrationsAsync()).ToArray());
        }
    }

    /// <summary>Nomes das entradas do zip (os mesmos de <c>PacoteBackup</c>, que é interno do Core).</summary>
    internal static class PacoteBackupNomes
    {
        public const string Banco = "comoestamos.db3";
        public const string Manifesto = "manifest.json";
    }
}

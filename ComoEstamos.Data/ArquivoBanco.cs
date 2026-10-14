using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data
{
    /// <summary>Registros ativos de cada tabela de um banco (o resumo mostrado antes de restaurar um backup).</summary>
    public record ContagemRegistros(
        int Contas,
        int Dividas,
        int Receitas,
        int Parcelas,
        int Categorias,
        int Credores,
        int Carteiras,
        int Investimentos);

    /// <summary>
    /// O arquivo SQLite do app, para o backup e o restore: copiar, conferir e substituir o banco inteiro.
    /// Os métodos que recebem um caminho trabalham num arquivo avulso (a cópia de um backup), sem pool de
    /// conexões, para o arquivo poder ser apagado logo depois.
    /// </summary>
    public interface IArquivoBanco
    {
        /// <summary>Caminho do banco em uso pelo app.</summary>
        string Caminho { get; }

        /// <summary>
        /// Copia o banco em uso para <paramref name="destino"/> (que não pode existir) com <c>VACUUM INTO</c>.
        /// Copiar o arquivo direto pode perder gravações que ainda estão no <c>-wal</c>.
        /// </summary>
        Task CopiarParaAsync(string destino);

        /// <summary>Se o arquivo é um banco SQLite íntegro (<c>PRAGMA integrity_check</c> = <c>ok</c>).</summary>
        Task<bool> EstaIntegroAsync(string caminho);

        /// <summary>Migrations aplicadas no banco do arquivo, em ordem (vazia se não for um banco do app).</summary>
        Task<List<string>> MigrationsAplicadasAsync(string caminho);

        /// <summary>Todas as migrations que esta versão do app conhece, em ordem.</summary>
        IReadOnlyList<string> MigrationsConhecidas { get; }

        Task<ContagemRegistros> ContarAsync(string caminho);

        /// <summary>
        /// Troca o banco em uso pelo arquivo <paramref name="origem"/> e aplica as migrations pendentes.
        /// Fecha antes as conexões do pool e apaga o <c>-wal</c>/<c>-shm</c> do banco antigo.
        /// </summary>
        Task SubstituirPorAsync(string origem);
    }

    public class ArquivoBanco(string caminho) : IArquivoBanco
    {
        public string Caminho { get; } = caminho;

        public IReadOnlyList<string> MigrationsConhecidas
        {
            get
            {
                using var db = CriarContexto(Caminho);
                return db.Database.GetMigrations().ToList();
            }
        }

        public async Task CopiarParaAsync(string destino)
        {
            await using var conexao = new SqliteConnection($"Data Source={Caminho}");
            await conexao.OpenAsync();
            await using var comando = conexao.CreateCommand();
            comando.CommandText = "VACUUM INTO $destino";
            comando.Parameters.AddWithValue("$destino", destino);
            await comando.ExecuteNonQueryAsync();
        }

        public async Task<bool> EstaIntegroAsync(string caminho)
        {
            try
            {
                await using var conexao = new SqliteConnection(TextoConexaoAvulsa(caminho, somenteLeitura: true));
                await conexao.OpenAsync();
                await using var comando = conexao.CreateCommand();
                comando.CommandText = "PRAGMA integrity_check";
                return await comando.ExecuteScalarAsync() as string == "ok";
            }
            catch (SqliteException)
            {
                // "file is not a database" e afins.
                return false;
            }
        }

        public async Task<List<string>> MigrationsAplicadasAsync(string caminho)
        {
            await using var db = CriarContexto(caminho, avulso: true);
            try
            {
                return (await db.Database.GetAppliedMigrationsAsync()).ToList();
            }
            catch (SqliteException)
            {
                return [];
            }
        }

        public async Task<ContagemRegistros> ContarAsync(string caminho)
        {
            await using var db = CriarContexto(caminho, avulso: true);
            return new ContagemRegistros(
                await db.Contas.CountAsync(c => c.Ativo),
                await db.Dividas.CountAsync(d => d.Ativo && d.EhDivida),
                await db.Dividas.CountAsync(d => d.Ativo && !d.EhDivida),
                await db.Parcelas.CountAsync(p => p.Ativo),
                await db.Categorias.CountAsync(c => c.Ativo),
                await db.Credores.CountAsync(c => c.Ativo),
                await db.Carteiras.CountAsync(c => c.Ativo),
                await db.Investimentos.CountAsync(i => i.Ativo));
        }

        public async Task SubstituirPorAsync(string origem)
        {
            // Nenhuma conexão do pool pode continuar aberta no arquivo antigo.
            SqliteConnection.ClearAllPools();
            File.Copy(origem, Caminho, overwrite: true);
            // O -wal do banco antigo seria aplicado ao novo e o corromperia.
            File.Delete(Caminho + "-wal");
            File.Delete(Caminho + "-shm");

            await using var db = CriarContexto(Caminho);
            await db.Database.MigrateAsync();
        }

        private static string TextoConexaoAvulsa(string caminho, bool somenteLeitura = false) =>
            new SqliteConnectionStringBuilder
            {
                DataSource = caminho,
                Pooling = false,
                Mode = somenteLeitura ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
            }.ToString();

        private static AppDbContext CriarContexto(string caminho, bool avulso = false) =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(avulso ? TextoConexaoAvulsa(caminho, somenteLeitura: true) : $"Data Source={caminho}")
                .Options);
    }
}

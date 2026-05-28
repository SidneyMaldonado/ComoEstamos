using SQLite;

namespace ComoEstamos.Data
{
    public class SaldoDatabase
    {
        private SQLiteAsyncConnection? _db;

        private async Task InicializarAsync()
        {
            if (_db is not null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "comoestamos.db3");
            _db = new SQLiteAsyncConnection(dbPath);
            await _db.CreateTableAsync<SaldoItem>();
        }

        public async Task SalvarAsync(string chave, decimal valor)
        {
            await InicializarAsync();
            await _db!.InsertOrReplaceAsync(new SaldoItem { Chave = chave, Valor = valor });
        }

        public async Task<Dictionary<string, decimal>> CarregarTodosAsync()
        {
            await InicializarAsync();
            var lista = await _db!.Table<SaldoItem>().ToListAsync();
            return lista.ToDictionary(x => x.Chave, x => x.Valor);
        }
    }
}

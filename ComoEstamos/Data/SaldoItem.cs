using SQLite;

namespace ComoEstamos.Data
{
    [Table("Saldos")]
    public class SaldoItem
    {
        [PrimaryKey]
        public string Chave { get; set; } = string.Empty;

        public decimal Valor { get; set; }
    }
}

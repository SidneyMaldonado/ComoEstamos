using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Model
{
    /// <summary>Saldo de uma conta em uma data, gravado a cada atualização de saldo.</summary>
    [Table("tb_historico_saldo")]
    public class HistoricoSaldo : EntidadeBase
    {
        [Key]
        [Column("id_historico_saldo")]
        public int Id { get; set; }

        [Column("dt_historico_saldo")]
        public DateTime DataHistoricoSaldo { get; set; } = DateTime.Now;

        [Column("id_conta")]
        public int IdConta { get; set; }

        [Column("nr_saldo")]
        [Precision(10, 2)]
        public decimal Saldo { get; set; }

        [ForeignKey(nameof(IdConta))]
        public Conta? Conta { get; set; }
    }
}

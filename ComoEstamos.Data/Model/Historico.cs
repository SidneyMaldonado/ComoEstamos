using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Model
{
    /// <summary>Retrato (snapshot) de um investimento em uma data.</summary>
    [Table("tb_historico")]
    public class Historico : EntidadeBase
    {
        [Key]
        [Column("id_historico")]
        public int Id { get; set; }

        [Column("dt_historico")]
        public DateTime DataHistorico { get; set; } = DateTime.Now;

        [Column("id_investimento")]
        public int IdInvestimento { get; set; }

        [Column("nm_investimento")]
        [MaxLength(50)]
        public string NomeInvestimento { get; set; } = string.Empty;

        [Column("nr_quantidade")]
        [Precision(18, 6)]
        public decimal Quantidade { get; set; }

        [Column("vl_cotacao")]
        [Precision(18, 6)]
        public decimal Cotacao { get; set; }

        [Column("ds_observacao")]
        [MaxLength(500)]
        public string? Observacao { get; set; }

        [ForeignKey(nameof(IdInvestimento))]
        public Investimento? Investimento { get; set; }
    }
}

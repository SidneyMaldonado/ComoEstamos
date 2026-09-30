using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Model
{
    [Table("tb_investimento")]
    public class Investimento : EntidadeBase
    {
        [Key]
        [Column("id_investimento")]
        public int Id { get; set; }

        [Column("id_carteira")]
        public int IdCarteira { get; set; }

        [Column("nm_investimento")]
        [MaxLength(50)]
        public string Nome { get; set; } = string.Empty;

        [Column("nr_quantidade")]
        [Precision(18, 6)]
        public decimal Quantidade { get; set; }

        [Column("vl_cotacao")]
        [Precision(18, 6)]
        public decimal Cotacao { get; set; }

        [Column("ds_observacao")]
        [MaxLength(500)]
        public string? Observacao { get; set; }

        [ForeignKey(nameof(IdCarteira))]
        public Carteira? Carteira { get; set; }

        public List<Operacao> Operacoes { get; set; } = [];
        public List<Historico> Historicos { get; set; } = [];
    }
}

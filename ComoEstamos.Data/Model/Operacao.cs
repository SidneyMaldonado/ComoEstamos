using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Model
{
    [Table("tb_operacao")]
    public class Operacao : EntidadeBase
    {
        [Key]
        [Column("id_operacao")]
        public int Id { get; set; }

        [Column("id_investimento")]
        public int IdInvestimento { get; set; }

        /// <summary>true = compra, false = venda.</summary>
        [Column("dm_compra")]
        public bool EhCompra { get; set; }

        [Column("dt_operacao")]
        public DateTime DataOperacao { get; set; } = DateTime.Now;

        [Column("nr_quantidade")]
        [Precision(18, 6)]
        public decimal Quantidade { get; set; }

        [Column("vl_operacao")]
        [Precision(10, 2)]
        public decimal Valor { get; set; }

        [ForeignKey(nameof(IdInvestimento))]
        public Investimento? Investimento { get; set; }
    }
}

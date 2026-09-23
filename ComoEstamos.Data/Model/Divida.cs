using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Model
{
    [Table("tb_divida")]
    public class Divida : EntidadeBase
    {
        [Key]
        [Column("id_divida")]
        public int Id { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("id_credor")]
        public int? IdCredor { get; set; }

        [Column("id_conta")]
        public int? IdConta { get; set; }

        [Column("id_categoria")]
        public int? IdCategoria { get; set; }

        [Column("nm_divida")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        /// <summary>Dia do mês (1 a 31) em que as parcelas vencem.</summary>
        [Column("dia_vencimento")]
        public int DiaVencimento { get; set; } = 10;

        [Column("dt_primeiro_vencimento")]
        public DateTime DataPrimeiroVencimento { get; set; }

        [Column("nr_parcelas")]
        public int NumeroParcelas { get; set; } = 1;

        [Column("nr_valor")]
        [Precision(10, 2)]
        public decimal Valor { get; set; }

        [Column("dm_divida")]
        public bool? EhDivida { get; set; } = true;

        [ForeignKey(nameof(IdUsuario))]
        public Usuario? Usuario { get; set; }

        [ForeignKey(nameof(IdCredor))]
        public Credor? Credor { get; set; }

        [ForeignKey(nameof(IdConta))]
        public Conta? Conta { get; set; }

        [ForeignKey(nameof(IdCategoria))]
        public Categoria? Categoria { get; set; }

        public List<Parcela> Parcelas { get; set; } = [];
    }
}

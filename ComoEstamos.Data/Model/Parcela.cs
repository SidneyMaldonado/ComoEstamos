using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Model
{
    [Table("tb_parcela")]
    public class Parcela : EntidadeBase
    {
        [Key]
        [Column("id_parcela")]
        public int Id { get; set; }

        [Column("id_divida")]
        public int IdDivida { get; set; }

        [Column("id_categoria")]
        public int IdCategoria { get; set; }

        [Column("id_conta")]
        public int IdConta { get; set; }

        [Column("ds_parcela")]
        [MaxLength(100)]
        public string Descricao { get; set; } = string.Empty;

        [Column("nr_valor")]
        [Precision(10, 2)]
        public decimal Valor { get; set; }

        [Column("dt_vencimento")]
        public DateTime DataVencimento { get; set; }

        [Column("dt_pagamento")]
        public DateTime? DataPagamento { get; set; }

        [ForeignKey(nameof(IdDivida))]
        public Divida? Divida { get; set; }

        [ForeignKey(nameof(IdCategoria))]
        public Categoria? Categoria { get; set; }

        [ForeignKey(nameof(IdConta))]
        public Conta? Conta { get; set; }
    }
}

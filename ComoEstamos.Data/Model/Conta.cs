using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Model
{
    [Table("tb_conta")]
    public class Conta : EntidadeBase
    {
        [Key]
        [Column("id_conta")]
        public int Id { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("nm_conta")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Column("img_conta")]
        public byte[]? Imagem { get; set; }

        [Column("nr_saldo")]
        [Precision(10, 2)]
        public decimal Saldo { get; set; }

        [ForeignKey(nameof(IdUsuario))]
        public Usuario? Usuario { get; set; }
    }
}

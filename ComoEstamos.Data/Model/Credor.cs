using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComoEstamos.Data.Model
{
    [Table("tb_credor")]
    public class Credor : EntidadeBase
    {
        [Key]
        [Column("id_credor")]
        public int Id { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("nm_credor")]
        [MaxLength(50)]
        public string Nome { get; set; } = string.Empty;

        [Column("ds_observacoes")]
        public string? Observacoes { get; set; }

        [Column("img_logo")]
        public byte[]? Logo { get; set; }

        [ForeignKey(nameof(IdUsuario))]
        public Usuario? Usuario { get; set; }
    }
}

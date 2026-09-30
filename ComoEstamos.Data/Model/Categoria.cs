using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComoEstamos.Data.Model
{
    [Table("tb_categoria")]
    public class Categoria : EntidadeBase
    {
        [Key]
        [Column("id_categoria")]
        public int Id { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("nm_categoria")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Column("img_categoria")]
        public byte[]? Imagem { get; set; }

        [ForeignKey(nameof(IdUsuario))]
        public Usuario? Usuario { get; set; }
    }
}

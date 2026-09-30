using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComoEstamos.Data.Model
{
    [Table("tb_carteira")]
    public class Carteira : EntidadeBase
    {
        [Key]
        [Column("id_carteira")]
        public int Id { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("nm_carteira")]
        [MaxLength(50)]
        public string Nome { get; set; } = string.Empty;

        [ForeignKey(nameof(IdUsuario))]
        public Usuario? Usuario { get; set; }

        public List<Investimento> Investimentos { get; set; } = [];
    }
}

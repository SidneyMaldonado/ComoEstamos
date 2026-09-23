using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Model
{
    [Table("tb_usuario")]
    [Index(nameof(Email), IsUnique = true, Name = "UQ_tb_usuario_ds_email")]
    public class Usuario : EntidadeBase
    {
        [Key]
        [Column("id_usuario")]
        public int Id { get; set; }

        [Column("nm_usuario")]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Column("ds_email")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        /// <summary>Hash PBKDF2 da senha — nunca a senha em texto puro.</summary>
        [Column("ds_senha")]
        [MaxLength(200)]
        public string SenhaHash { get; set; } = string.Empty;

        [Column("img_usuario")]
        public byte[]? Imagem { get; set; }

        public List<Carteira> Carteiras { get; set; } = [];
        public List<Categoria> Categorias { get; set; } = [];
        public List<Conta> Contas { get; set; } = [];
        public List<Credor> Credores { get; set; } = [];
        public List<Divida> Dividas { get; set; } = [];
    }
}

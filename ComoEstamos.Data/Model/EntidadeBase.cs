using System.ComponentModel.DataAnnotations.Schema;

namespace ComoEstamos.Data.Model
{
    /// <summary>
    /// Campos comuns a todas as tabelas: exclusão lógica e auditoria.
    /// As datas de auditoria são preenchidas pelo AppDbContext ao salvar.
    /// </summary>
    public abstract class EntidadeBase
    {
        [Column("dm_ativo")]
        public bool Ativo { get; set; } = true;

        [Column("dt_criacao")]
        public DateTime DataCriacao { get; set; }

        [Column("dt_alteracao")]
        public DateTime? DataAlteracao { get; set; }
    }
}

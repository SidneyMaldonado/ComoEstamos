using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Carteira> Carteiras => Set<Carteira>();
        public DbSet<Categoria> Categorias => Set<Categoria>();
        public DbSet<Conta> Contas => Set<Conta>();
        public DbSet<Credor> Credores => Set<Credor>();
        public DbSet<Divida> Dividas => Set<Divida>();
        public DbSet<Parcela> Parcelas => Set<Parcela>();
        public DbSet<Investimento> Investimentos => Set<Investimento>();
        public DbSet<Operacao> Operacoes => Set<Operacao>();
        public DbSet<Historico> Historicos => Set<Historico>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Divida>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_tb_divida_dia_vencimento", "dia_vencimento >= 1 AND dia_vencimento <= 31");
                // O SQLite grava decimal como TEXT, por isso o CAST para comparar como número.
                t.HasCheckConstraint("CK_tb_divida_nr_valor", "CAST(nr_valor AS REAL) >= 0");
            });

            // Como no SQL Server original: nenhuma exclusão em cascata.
            // Os registros são desativados (dm_ativo = 0), não apagados.
            foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            PreencherAuditoria();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            PreencherAuditoria();
            return base.SaveChanges();
        }

        private void PreencherAuditoria()
        {
            var agora = DateTime.Now;
            foreach (var entry in ChangeTracker.Entries<EntidadeBase>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.DataCriacao = agora;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.DataAlteracao = agora;
                    entry.Property(e => e.DataCriacao).IsModified = false;
                }
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ComoEstamos.Data
{
    /// <summary>
    /// Usada apenas pelo "dotnet ef" para gerar migrations.
    /// Em tempo de execução, o app configura o caminho real do banco no MauiProgram.
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=comoestamos_design.db3")
                .Options;

            return new AppDbContext(options);
        }
    }
}

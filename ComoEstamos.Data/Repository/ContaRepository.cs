using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IContaRepository : IRepository<Conta>
    {
        Task<List<Conta>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false);
        Task<decimal> ObterSaldoTotalAsync(int idUsuario);
        Task AtualizarSaldoAsync(int idConta, decimal novoSaldo);
    }

    public class ContaRepository(IDbContextFactory<AppDbContext> contextFactory)
        : RepositoryBase<Conta>(contextFactory), IContaRepository
    {
        public Task<List<Conta>> ListarPorUsuarioAsync(int idUsuario, bool incluirInativos = false) =>
            ListarAsync(c => c.IdUsuario == idUsuario, incluirInativos, q => q.OrderBy(c => c.Nome));

        public async Task<decimal> ObterSaldoTotalAsync(int idUsuario)
        {
            // O SQLite guarda decimal como texto; a soma é feita em memória para não perder precisão.
            var contas = await ListarPorUsuarioAsync(idUsuario);
            return contas.Sum(c => c.Saldo);
        }

        public async Task AtualizarSaldoAsync(int idConta, decimal novoSaldo)
        {
            await using var db = await ContextFactory.CreateDbContextAsync();
            var conta = await db.Contas.FindAsync(idConta)
                ?? throw new KeyNotFoundException($"Conta {idConta} não encontrada.");
            conta.Saldo = novoSaldo;
            await db.SaveChangesAsync();
        }
    }
}

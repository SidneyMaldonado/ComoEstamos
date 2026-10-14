using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IHistoricoSaldoRepository : IRepository<HistoricoSaldo>
    {
        /// <summary>Históricos da conta, do mais recente para o mais antigo.</summary>
        Task<List<HistoricoSaldo>> ListarPorContaAsync(int idConta, bool incluirInativos = false);

        /// <summary>Desativa (exclusão lógica) todos os históricos ativos da conta. Devolve quantos foram desativados.</summary>
        Task<int> DesativarPorContaAsync(int idConta);
    }

    public class HistoricoSaldoRepository : RepositoryBase<HistoricoSaldo>, IHistoricoSaldoRepository
    {
        public HistoricoSaldoRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

        internal HistoricoSaldoRepository(AppDbContext contexto) : base(contexto) { }

        public Task<List<HistoricoSaldo>> ListarPorContaAsync(int idConta, bool incluirInativos = false) =>
            ListarAsync(h => h.IdConta == idConta, incluirInativos, q => q.OrderByDescending(h => h.DataHistoricoSaldo));

        public async Task<int> DesativarPorContaAsync(int idConta)
        {
            await using var ctx = await AbrirContextoAsync();
            var historicos = await ctx.Db.HistoricosSaldo.Where(h => h.IdConta == idConta && h.Ativo).ToListAsync();
            foreach (var historico in historicos)
                historico.Ativo = false;
            await ctx.SalvarAsync();
            return historicos.Count;
        }
    }
}

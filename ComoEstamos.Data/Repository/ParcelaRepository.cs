using ComoEstamos.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace ComoEstamos.Data.Repository
{
    public interface IParcelaRepository : IRepository<Parcela>
    {
        Task<List<Parcela>> ListarPorDividaAsync(int idDivida, bool incluirInativos = false);

        /// <summary>Parcelas ainda não pagas do usuário que vencem até a data informada.</summary>
        Task<List<Parcela>> ListarPendentesAsync(int idUsuario, DateTime vencimentoAte);

        Task RegistrarPagamentoAsync(int idParcela, DateTime dataPagamento);
    }

    public class ParcelaRepository : RepositoryBase<Parcela>, IParcelaRepository
    {
        public ParcelaRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

        internal ParcelaRepository(AppDbContext contexto) : base(contexto) { }

        public Task<List<Parcela>> ListarPorDividaAsync(int idDivida, bool incluirInativos = false) =>
            ListarAsync(p => p.IdDivida == idDivida, incluirInativos, q => q.OrderBy(p => p.DataVencimento));

        public Task<List<Parcela>> ListarPendentesAsync(int idUsuario, DateTime vencimentoAte) =>
            ListarAsync(
                p => p.Divida!.IdUsuario == idUsuario
                     && p.Divida.Ativo
                     && p.DataPagamento == null
                     && p.DataVencimento <= vencimentoAte,
                montarConsulta: q => q
                    .Include(p => p.Divida)
                    .Include(p => p.Conta)
                    .Include(p => p.Categoria)
                    .OrderBy(p => p.DataVencimento));

        public async Task RegistrarPagamentoAsync(int idParcela, DateTime dataPagamento)
        {
            await using var ctx = await AbrirContextoAsync();
            var parcela = await ctx.Db.Parcelas.FindAsync(idParcela)
                ?? throw new KeyNotFoundException($"Parcela {idParcela} não encontrada.");
            parcela.DataPagamento = dataPagamento;
            await ctx.SalvarAsync();
        }
    }
}

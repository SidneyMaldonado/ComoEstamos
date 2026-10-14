using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.Services
{
    /// <summary>Operações de dívida usadas pelas telas, sempre do usuário atual.</summary>
    public interface IDividaService
    {
        /// <summary>Consulta das dívidas ou, com <see cref="FiltroDividas.EhDivida"/> false, das receitas; filtrada e ordenada pelo vencimento.</summary>
        Task<List<ResumoDivida>> BuscarAsync(FiltroDividas filtro);
        Task<Divida?> BuscarPorIdAsync(int id);

        /// <summary>Dívidas e créditos com parcela vencendo no mês, com os totais e a diferença.</summary>
        Task<ResumoMes> BuscarResumoMesAsync(DateTime mes);

        /// <summary>Grava a dívida e gera as parcelas.</summary>
        Task<Divida> IncluirAsync(Divida divida);

        /// <summary>Grava os campos editáveis; as parcelas em aberto são refeitas se um campo financeiro mudou.</summary>
        Task AlterarAsync(Divida divida);

        /// <summary>Exclui a dívida e as parcelas; o saldo das contas não muda.</summary>
        Task ExcluirAsync(int id);
    }

    public class DividaService(
        IUsuarioAtual usuarioAtual,
        IBuscarDividasUseCase buscar,
        IBuscarDividaPorIdUseCase buscarPorId,
        IBuscarResumoMesUseCase buscarResumoMes,
        ICriarDividaUseCase criar,
        IAlterarDividaUseCase alterar,
        IExcluirDividaUseCase excluir) : IDividaService
    {
        public async Task<List<ResumoDivida>> BuscarAsync(FiltroDividas filtro) =>
            await buscar.ExecutarAsync(filtro, await usuarioAtual.ObterIdAsync());

        public async Task<Divida?> BuscarPorIdAsync(int id) =>
            await buscarPorId.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());

        public async Task<ResumoMes> BuscarResumoMesAsync(DateTime mes) =>
            await buscarResumoMes.ExecutarAsync(mes, await usuarioAtual.ObterIdAsync());

        public async Task<Divida> IncluirAsync(Divida divida) =>
            await criar.ExecutarAsync(divida, await usuarioAtual.ObterIdAsync());

        public async Task AlterarAsync(Divida divida) =>
            await alterar.ExecutarAsync(divida, await usuarioAtual.ObterIdAsync());

        public async Task ExcluirAsync(int id) =>
            await excluir.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());
    }
}

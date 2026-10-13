using ComoEstamos.Core.UseCases.Investimentos;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.Services
{
    /// <summary>Operações de investimento usadas pelas telas, sempre nas carteiras do usuário atual.</summary>
    public interface IInvestimentoService
    {
        Task<List<Investimento>> BuscarTodosAsync();
        Task<Investimento?> BuscarPorIdAsync(int id);
        Task<Investimento> IncluirAsync(Investimento investimento);
        Task AlterarAsync(Investimento investimento);
        Task ExcluirAsync(int id);
    }

    public class InvestimentoService(
        IUsuarioAtual usuarioAtual,
        IBuscarTodosInvestimentosUseCase buscarTodos,
        IBuscarInvestimentoPorIdUseCase buscarPorId,
        IIncluirInvestimentoUseCase incluir,
        IAlterarInvestimentoUseCase alterar,
        IExcluirInvestimentoUseCase excluir) : IInvestimentoService
    {
        public async Task<List<Investimento>> BuscarTodosAsync() =>
            await buscarTodos.ExecutarAsync(await usuarioAtual.ObterIdAsync());

        public async Task<Investimento?> BuscarPorIdAsync(int id) =>
            await buscarPorId.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());

        public async Task<Investimento> IncluirAsync(Investimento investimento) =>
            await incluir.ExecutarAsync(investimento, await usuarioAtual.ObterIdAsync());

        public async Task AlterarAsync(Investimento investimento) =>
            await alterar.ExecutarAsync(investimento, await usuarioAtual.ObterIdAsync());

        public async Task ExcluirAsync(int id) =>
            await excluir.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());
    }
}

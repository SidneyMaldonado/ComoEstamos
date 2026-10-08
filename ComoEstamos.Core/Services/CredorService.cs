using ComoEstamos.Core.UseCases.Credores;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.Services
{
    /// <summary>Operações de credor usadas pelas telas, sempre do usuário atual.</summary>
    public interface ICredorService
    {
        Task<List<Credor>> BuscarTodosAsync();
        Task<Credor?> BuscarPorIdAsync(int id);
        Task<Credor> IncluirAsync(Credor credor);
        Task AlterarAsync(Credor credor);
        Task ExcluirAsync(int id);
    }

    public class CredorService(
        IUsuarioAtual usuarioAtual,
        IBuscarTodosCredoresUseCase buscarTodos,
        IBuscarCredorPorIdUseCase buscarPorId,
        IIncluirCredorUseCase incluir,
        IAlterarCredorUseCase alterar,
        IExcluirCredorUseCase excluir) : ICredorService
    {
        public async Task<List<Credor>> BuscarTodosAsync() =>
            await buscarTodos.ExecutarAsync(await usuarioAtual.ObterIdAsync());

        public async Task<Credor?> BuscarPorIdAsync(int id) =>
            await buscarPorId.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());

        public async Task<Credor> IncluirAsync(Credor credor) =>
            await incluir.ExecutarAsync(credor, await usuarioAtual.ObterIdAsync());

        public async Task AlterarAsync(Credor credor) =>
            await alterar.ExecutarAsync(credor, await usuarioAtual.ObterIdAsync());

        public async Task ExcluirAsync(int id) =>
            await excluir.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());
    }
}

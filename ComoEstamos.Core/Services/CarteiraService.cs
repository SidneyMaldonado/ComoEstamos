using ComoEstamos.Core.UseCases.Carteiras;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.Services
{
    /// <summary>Operações de carteira usadas pelas telas, sempre do usuário atual.</summary>
    public interface ICarteiraService
    {
        Task<List<Carteira>> BuscarTodasAsync();
        Task<Carteira?> BuscarPorIdAsync(int id);
        Task<Carteira> IncluirAsync(Carteira carteira);
        Task AlterarAsync(Carteira carteira);
        Task ExcluirAsync(int id);
    }

    public class CarteiraService(
        IUsuarioAtual usuarioAtual,
        IBuscarTodasCarteirasUseCase buscarTodas,
        IBuscarCarteiraPorIdUseCase buscarPorId,
        IIncluirCarteiraUseCase incluir,
        IAlterarCarteiraUseCase alterar,
        IExcluirCarteiraUseCase excluir) : ICarteiraService
    {
        public async Task<List<Carteira>> BuscarTodasAsync() =>
            await buscarTodas.ExecutarAsync(await usuarioAtual.ObterIdAsync());

        public async Task<Carteira?> BuscarPorIdAsync(int id) =>
            await buscarPorId.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());

        public async Task<Carteira> IncluirAsync(Carteira carteira) =>
            await incluir.ExecutarAsync(carteira, await usuarioAtual.ObterIdAsync());

        public async Task AlterarAsync(Carteira carteira) =>
            await alterar.ExecutarAsync(carteira, await usuarioAtual.ObterIdAsync());

        public async Task ExcluirAsync(int id) =>
            await excluir.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());
    }
}

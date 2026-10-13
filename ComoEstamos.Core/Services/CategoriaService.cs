using ComoEstamos.Core.UseCases.Categorias;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.Services
{
    /// <summary>Operações de categoria usadas pelas telas, sempre do usuário atual.</summary>
    public interface ICategoriaService
    {
        Task<List<Categoria>> BuscarTodasAsync();
        Task<Categoria?> BuscarPorIdAsync(int id);
        Task<Categoria> IncluirAsync(Categoria categoria);
        Task AlterarAsync(Categoria categoria);
        Task ExcluirAsync(int id);
    }

    public class CategoriaService(
        IUsuarioAtual usuarioAtual,
        IBuscarTodasCategoriasUseCase buscarTodas,
        IBuscarCategoriaPorIdUseCase buscarPorId,
        IIncluirCategoriaUseCase incluir,
        IAlterarCategoriaUseCase alterar,
        IExcluirCategoriaUseCase excluir) : ICategoriaService
    {
        public async Task<List<Categoria>> BuscarTodasAsync() =>
            await buscarTodas.ExecutarAsync(await usuarioAtual.ObterIdAsync());

        public async Task<Categoria?> BuscarPorIdAsync(int id) =>
            await buscarPorId.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());

        public async Task<Categoria> IncluirAsync(Categoria categoria) =>
            await incluir.ExecutarAsync(categoria, await usuarioAtual.ObterIdAsync());

        public async Task AlterarAsync(Categoria categoria) =>
            await alterar.ExecutarAsync(categoria, await usuarioAtual.ObterIdAsync());

        public async Task ExcluirAsync(int id) =>
            await excluir.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());
    }
}

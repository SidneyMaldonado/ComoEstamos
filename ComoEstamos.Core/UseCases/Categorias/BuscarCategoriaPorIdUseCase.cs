using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Categorias
{
    public interface IBuscarCategoriaPorIdUseCase
    {
        /// <summary>A categoria ativa do usuário, ou <c>null</c> se não existe, é de outro usuário ou foi excluída.</summary>
        Task<Categoria?> ExecutarAsync(int id, int idUsuario);
    }

    public class BuscarCategoriaPorIdUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarCategoriaPorIdUseCase
    {
        public async Task<Categoria?> ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            var categoria = await uow.Categorias.ObterPorIdAsync(id);
            return categoria is { Ativo: true } && categoria.IdUsuario == idUsuario ? categoria : null;
        }
    }
}

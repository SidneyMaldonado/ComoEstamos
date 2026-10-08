using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Categorias
{
    public interface IBuscarTodasCategoriasUseCase
    {
        /// <summary>Categorias ativas do usuário, por nome.</summary>
        Task<List<Categoria>> ExecutarAsync(int idUsuario);
    }

    public class BuscarTodasCategoriasUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarTodasCategoriasUseCase
    {
        public async Task<List<Categoria>> ExecutarAsync(int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            return await uow.Categorias.ListarPorUsuarioAsync(idUsuario);
        }
    }
}

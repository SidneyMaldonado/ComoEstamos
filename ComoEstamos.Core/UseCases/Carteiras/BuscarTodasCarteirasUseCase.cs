using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Carteiras
{
    public interface IBuscarTodasCarteirasUseCase
    {
        /// <summary>Carteiras ativas do usuário, por nome.</summary>
        Task<List<Carteira>> ExecutarAsync(int idUsuario);
    }

    public class BuscarTodasCarteirasUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarTodasCarteirasUseCase
    {
        public async Task<List<Carteira>> ExecutarAsync(int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            return await uow.Carteiras.ListarPorUsuarioAsync(idUsuario);
        }
    }
}

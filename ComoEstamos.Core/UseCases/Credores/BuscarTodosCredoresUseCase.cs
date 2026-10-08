using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Credores
{
    public interface IBuscarTodosCredoresUseCase
    {
        /// <summary>Credores ativos do usuário, por nome.</summary>
        Task<List<Credor>> ExecutarAsync(int idUsuario);
    }

    public class BuscarTodosCredoresUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarTodosCredoresUseCase
    {
        public async Task<List<Credor>> ExecutarAsync(int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            return await uow.Credores.ListarPorUsuarioAsync(idUsuario);
        }
    }
}

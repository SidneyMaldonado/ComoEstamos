using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Contas
{
    public interface IBuscarTodasContasUseCase
    {
        /// <summary>Contas ativas do usuário, por nome.</summary>
        Task<List<Conta>> ExecutarAsync(int idUsuario);
    }

    public class BuscarTodasContasUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarTodasContasUseCase
    {
        public async Task<List<Conta>> ExecutarAsync(int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            return await uow.Contas.ListarPorUsuarioAsync(idUsuario);
        }
    }
}

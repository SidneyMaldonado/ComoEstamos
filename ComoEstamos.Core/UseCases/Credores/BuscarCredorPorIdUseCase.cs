using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Credores
{
    public interface IBuscarCredorPorIdUseCase
    {
        /// <summary>O credor ativo do usuário, ou <c>null</c> se não existe, é de outro usuário ou foi excluído.</summary>
        Task<Credor?> ExecutarAsync(int id, int idUsuario);
    }

    public class BuscarCredorPorIdUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarCredorPorIdUseCase
    {
        public async Task<Credor?> ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            var credor = await uow.Credores.ObterPorIdAsync(id);
            return credor is { Ativo: true } && credor.IdUsuario == idUsuario ? credor : null;
        }
    }
}

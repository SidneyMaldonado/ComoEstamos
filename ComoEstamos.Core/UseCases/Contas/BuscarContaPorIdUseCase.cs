using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Contas
{
    public interface IBuscarContaPorIdUseCase
    {
        /// <summary>A conta ativa do usuário, ou <c>null</c> se não existe, é de outro usuário ou foi excluída.</summary>
        Task<Conta?> ExecutarAsync(int id, int idUsuario);
    }

    public class BuscarContaPorIdUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarContaPorIdUseCase
    {
        public async Task<Conta?> ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            var conta = await uow.Contas.ObterPorIdAsync(id);
            return conta is { Ativo: true } && conta.IdUsuario == idUsuario ? conta : null;
        }
    }
}

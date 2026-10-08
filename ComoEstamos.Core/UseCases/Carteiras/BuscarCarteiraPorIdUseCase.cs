using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Carteiras
{
    public interface IBuscarCarteiraPorIdUseCase
    {
        /// <summary>A carteira ativa do usuário, ou <c>null</c> se não existe, é de outro usuário ou foi excluída.</summary>
        Task<Carteira?> ExecutarAsync(int id, int idUsuario);
    }

    public class BuscarCarteiraPorIdUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarCarteiraPorIdUseCase
    {
        public async Task<Carteira?> ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            var carteira = await uow.Carteiras.ObterPorIdAsync(id);
            return carteira is { Ativo: true } && carteira.IdUsuario == idUsuario ? carteira : null;
        }
    }
}

using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Carteiras
{
    public interface IAlterarCarteiraUseCase
    {
        /// <summary>Valida e grava o nome da carteira. Os demais campos gravados são mantidos.</summary>
        Task ExecutarAsync(Carteira carteira, int idUsuario);
    }

    public class AlterarCarteiraUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IAlterarCarteiraUseCase
    {
        public async Task ExecutarAsync(Carteira carteira, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var gravada = await CarteiraValidador.ObterAtivaDoUsuarioAsync(uow, carteira.Id, idUsuario);
            carteira.IdUsuario = idUsuario;
            await CarteiraValidador.ValidarAsync(uow, carteira);

            gravada.Nome = carteira.Nome;
            await uow.ConfirmarAsync();
        }
    }
}

using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Carteiras
{
    public interface IIncluirCarteiraUseCase
    {
        /// <summary>Valida e grava uma carteira nova, ativa, do usuário informado.</summary>
        Task<Carteira> ExecutarAsync(Carteira carteira, int idUsuario);
    }

    public class IncluirCarteiraUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IIncluirCarteiraUseCase
    {
        public async Task<Carteira> ExecutarAsync(Carteira carteira, int idUsuario)
        {
            carteira.Id = 0;
            carteira.IdUsuario = idUsuario;
            carteira.Ativo = true;

            await using var uow = await unitOfWorkFactory.IniciarAsync();
            await CarteiraValidador.ValidarAsync(uow, carteira);

            await uow.Carteiras.InserirAsync(carteira);
            await uow.ConfirmarAsync();
            return carteira;
        }
    }
}

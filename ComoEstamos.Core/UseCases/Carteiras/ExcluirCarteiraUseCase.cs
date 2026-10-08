using ComoEstamos.Data;

namespace ComoEstamos.Core.UseCases.Carteiras
{
    public interface IExcluirCarteiraUseCase
    {
        /// <summary>Desativa a carteira. Recusa se houver investimentos ativos nela.</summary>
        Task ExecutarAsync(int id, int idUsuario);
    }

    public class ExcluirCarteiraUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IExcluirCarteiraUseCase
    {
        public async Task ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            await CarteiraValidador.ObterAtivaDoUsuarioAsync(uow, id, idUsuario);
            if (await uow.Investimentos.ExisteAtivoPorCarteiraAsync(id))
                throw new RegraDeNegocioException("Não é possível excluir: existem investimentos ativos nesta carteira.");

            await uow.Carteiras.DesativarAsync(id);
            await uow.ConfirmarAsync();
        }
    }
}

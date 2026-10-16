using ComoEstamos.Data;

namespace ComoEstamos.Core.UseCases.Categorias
{
    public interface IExcluirCategoriaUseCase
    {
        /// <summary>Desativa a categoria. Recusa se houver dívidas ativas nela.</summary>
        Task ExecutarAsync(int id, int idUsuario);
    }

    public class ExcluirCategoriaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IExcluirCategoriaUseCase
    {
        public async Task ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            await CategoriaValidador.ObterAtivaDoUsuarioAsync(uow, id, idUsuario);
            if (await uow.Dividas.ExisteAtivaPorCategoriaAsync(id))
                throw new RegraDeNegocioException("Não é possível excluir: existem dívidas ativas nesta categoria.");

            await uow.Categorias.DesativarAsync(id);
            await uow.ConfirmarAsync();
        }
    }
}

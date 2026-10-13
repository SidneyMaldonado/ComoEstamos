using ComoEstamos.Data;

namespace ComoEstamos.Core.UseCases.Credores
{
    public interface IExcluirCredorUseCase
    {
        /// <summary>Desativa o credor. Recusa se houver dívidas ativas dele.</summary>
        Task ExecutarAsync(int id, int idUsuario);
    }

    public class ExcluirCredorUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IExcluirCredorUseCase
    {
        public async Task ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            await CredorValidador.ObterAtivoDoUsuarioAsync(uow, id, idUsuario);
            if (await uow.Dividas.ExisteAtivaPorCredorAsync(id))
                throw new RegraDeNegocioException("Não é possível excluir: existem dívidas ativas deste credor.");

            await uow.Credores.DesativarAsync(id);
            await uow.ConfirmarAsync();
        }
    }
}

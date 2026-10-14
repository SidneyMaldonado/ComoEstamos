using ComoEstamos.Data;

namespace ComoEstamos.Core.UseCases.Contas
{
    public interface IExcluirContaUseCase
    {
        /// <summary>
        /// Desativa a conta e o histórico de saldo dela. Recusa se houver dívidas ou parcelas ativas nela
        /// (o histórico não impede).
        /// </summary>
        Task ExecutarAsync(int id, int idUsuario);
    }

    public class ExcluirContaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IExcluirContaUseCase
    {
        public async Task ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            await ContaValidador.ObterAtivaDoUsuarioAsync(uow, id, idUsuario);
            if (await uow.Dividas.ExisteAtivaPorContaAsync(id))
                throw new RegraDeNegocioException("Não é possível excluir: existem dívidas ativas nesta conta.");
            if (await uow.Parcelas.ExisteAtivaPorContaAsync(id))
                throw new RegraDeNegocioException("Não é possível excluir: existem parcelas ativas nesta conta.");

            await uow.Contas.DesativarAsync(id);
            await uow.HistoricosSaldo.DesativarPorContaAsync(id);
            await uow.ConfirmarAsync();
        }
    }
}

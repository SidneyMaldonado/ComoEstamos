using ComoEstamos.Data;

namespace ComoEstamos.Core.UseCases.Investimentos
{
    public interface IExcluirInvestimentoUseCase
    {
        /// <summary>Desativa o investimento. Recusa se houver operações ou históricos ativos dele.</summary>
        Task ExecutarAsync(int id, int idUsuario);
    }

    public class ExcluirInvestimentoUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IExcluirInvestimentoUseCase
    {
        public async Task ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            await InvestimentoValidador.ObterAtivoDoUsuarioAsync(uow, id, idUsuario);
            if (await uow.Operacoes.ExisteAtivaPorInvestimentoAsync(id))
                throw new RegraDeNegocioException("Não é possível excluir: existem operações ativas deste investimento.");
            if (await uow.Historicos.ExisteAtivoPorInvestimentoAsync(id))
                throw new RegraDeNegocioException("Não é possível excluir: existem históricos ativos deste investimento.");

            await uow.Investimentos.DesativarAsync(id);
            await uow.ConfirmarAsync();
        }
    }
}

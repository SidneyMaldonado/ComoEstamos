using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Investimentos
{
    public interface IBuscarInvestimentoPorIdUseCase
    {
        /// <summary>
        /// O investimento ativo do usuário, com a carteira carregada, ou <c>null</c> se não existe,
        /// é de outro usuário ou foi excluído.
        /// </summary>
        Task<Investimento?> ExecutarAsync(int id, int idUsuario);
    }

    public class BuscarInvestimentoPorIdUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarInvestimentoPorIdUseCase
    {
        public async Task<Investimento?> ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            var investimento = await InvestimentoValidador.ObterDoUsuarioAsync(uow, id, idUsuario);
            return investimento is { Ativo: true } ? investimento : null;
        }
    }
}

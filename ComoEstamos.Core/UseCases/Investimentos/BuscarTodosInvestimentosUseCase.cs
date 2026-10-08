using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Investimentos
{
    public interface IBuscarTodosInvestimentosUseCase
    {
        /// <summary>Investimentos ativos das carteiras do usuário, com a carteira carregada, por carteira e nome.</summary>
        Task<List<Investimento>> ExecutarAsync(int idUsuario);
    }

    public class BuscarTodosInvestimentosUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarTodosInvestimentosUseCase
    {
        public async Task<List<Investimento>> ExecutarAsync(int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            return await uow.Investimentos.ListarPorUsuarioAsync(idUsuario);
        }
    }
}

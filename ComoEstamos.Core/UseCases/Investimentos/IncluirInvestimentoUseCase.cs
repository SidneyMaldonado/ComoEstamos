using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Investimentos
{
    public interface IIncluirInvestimentoUseCase
    {
        /// <summary>Valida e grava um investimento novo, ativo, numa carteira do usuário informado.</summary>
        Task<Investimento> ExecutarAsync(Investimento investimento, int idUsuario);
    }

    public class IncluirInvestimentoUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IIncluirInvestimentoUseCase
    {
        public async Task<Investimento> ExecutarAsync(Investimento investimento, int idUsuario)
        {
            investimento.Id = 0;
            investimento.Ativo = true;

            await using var uow = await unitOfWorkFactory.IniciarAsync();
            await InvestimentoValidador.ValidarAsync(uow, investimento, idUsuario);

            await uow.Investimentos.InserirAsync(investimento);
            await uow.ConfirmarAsync();
            return investimento;
        }
    }
}

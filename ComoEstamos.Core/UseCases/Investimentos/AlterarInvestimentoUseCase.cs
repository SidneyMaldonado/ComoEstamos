using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Investimentos
{
    public interface IAlterarInvestimentoUseCase
    {
        /// <summary>
        /// Valida e grava carteira, nome, quantidade, cotação e observação do investimento.
        /// Os demais campos gravados são mantidos.
        /// </summary>
        Task ExecutarAsync(Investimento investimento, int idUsuario);
    }

    public class AlterarInvestimentoUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IAlterarInvestimentoUseCase
    {
        public async Task ExecutarAsync(Investimento investimento, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var gravado = await InvestimentoValidador.ObterAtivoDoUsuarioAsync(uow, investimento.Id, idUsuario);
            await InvestimentoValidador.ValidarAsync(uow, investimento, idUsuario);

            gravado.IdCarteira = investimento.IdCarteira;
            gravado.Nome = investimento.Nome;
            gravado.Quantidade = investimento.Quantidade;
            gravado.Cotacao = investimento.Cotacao;
            gravado.Observacao = investimento.Observacao;
            await uow.ConfirmarAsync();
        }
    }
}

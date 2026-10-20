using ComoEstamos.Core.UseCases.Investimentos;
using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;

namespace ComoEstamos.Core.UseCases.Operacoes
{
    public interface IExcluirOperacaoUseCase
    {
        /// <summary>
        /// Desativa a operação e desfaz o efeito dela na quantidade do investimento: a compra excluída
        /// subtrai, a venda excluída soma. Recusa se a quantidade ficasse negativa.
        /// </summary>
        Task ExecutarAsync(int id, int idUsuario);
    }

    public class ExcluirOperacaoUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IExcluirOperacaoUseCase
    {
        public async Task ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var operacao = await uow.Operacoes.ObterPorIdAsync(id);
            var investimento = operacao is null ? null : await InvestimentoValidador.ObterDoUsuarioAsync(uow, operacao.IdInvestimento, idUsuario);
            if (operacao is null || investimento is null)
                throw new KeyNotFoundException("A operação não foi encontrada.");
            if (!operacao.Ativo)
                throw new RegraDeNegocioException("A operação já foi excluída.");
            if (!investimento.Ativo)
                throw new RegraDeNegocioException("O investimento desta operação foi excluído.");

            var quantidade = investimento.Quantidade + (operacao.EhCompra ? -operacao.Quantidade : operacao.Quantidade);
            if (quantidade < 0)
                throw new RegraDeNegocioException("Não é possível excluir: a quantidade do investimento ficaria negativa.");
            if (quantidade > Limites.Decimal18x6)
                throw new RegraDeNegocioException("Não é possível excluir: a quantidade do investimento passaria do limite.");

            investimento.Quantidade = quantidade;
            operacao.Ativo = false;

            await uow.ConfirmarAsync();
        }
    }
}

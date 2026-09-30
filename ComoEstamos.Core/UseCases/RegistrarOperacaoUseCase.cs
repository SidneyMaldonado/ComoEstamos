using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases
{
    public interface IRegistrarOperacaoUseCase
    {
        /// <summary>
        /// Registra a compra ou venda e atualiza a quantidade do investimento:
        /// soma na compra, subtrai na venda.
        /// </summary>
        Task<Operacao> ExecutarAsync(Operacao operacao);
    }

    public class RegistrarOperacaoUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IRegistrarOperacaoUseCase
    {
        public async Task<Operacao> ExecutarAsync(Operacao operacao)
        {
            if (operacao.Quantidade <= 0)
                throw new RegraDeNegocioException("A quantidade deve ser maior que zero.");
            if (operacao.Valor < 0)
                throw new RegraDeNegocioException("O valor não pode ser negativo.");

            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var investimento = await uow.Investimentos.ObterPorIdAsync(operacao.IdInvestimento)
                ?? throw new KeyNotFoundException($"Investimento {operacao.IdInvestimento} não encontrado.");
            if (!investimento.Ativo)
                throw new RegraDeNegocioException("O investimento está desativado.");
            if (!operacao.EhCompra && operacao.Quantidade > investimento.Quantidade)
                throw new RegraDeNegocioException(
                    $"Quantidade vendida ({operacao.Quantidade}) maior que a disponível ({investimento.Quantidade}).");

            investimento.Quantidade += operacao.EhCompra ? operacao.Quantidade : -operacao.Quantidade;

            await uow.Investimentos.AtualizarAsync(investimento);
            await uow.Operacoes.InserirAsync(operacao);
            await uow.ConfirmarAsync();
            return operacao;
        }
    }
}

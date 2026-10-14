using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Parcelas
{
    public interface IPagarParcelaUseCase
    {
        /// <summary>
        /// Marca a parcela como paga e ajusta o saldo da conta dela: debita se for
        /// dívida, credita se for crédito (valor a receber). A data não pode ser futura.
        /// </summary>
        Task ExecutarAsync(int idParcela, DateTime dataPagamento, int idUsuario);
    }

    public class PagarParcelaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IPagarParcelaUseCase
    {
        public async Task ExecutarAsync(int idParcela, DateTime dataPagamento, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var (parcela, divida) = await ParcelaValidador.ObterAtivaDoUsuarioAsync(uow, idParcela, idUsuario);
            if (parcela.DataPagamento is not null)
                throw new RegraDeNegocioException(divida.EhDivida ? "A parcela já está paga." : "A parcela já foi recebida.");

            var conta = await uow.Contas.ObterPorIdAsync(parcela.IdConta)
                ?? throw new KeyNotFoundException($"Conta {parcela.IdConta} não encontrada.");
            if (!conta.Ativo)
                throw new RegraDeNegocioException("A conta desta parcela está desativada.");

            if (dataPagamento.Date > DateTime.Today)
                throw new ValidacaoException([new ErroValidacao(nameof(Parcela.DataPagamento), divida.EhDivida ? "A data do pagamento não pode ser futura." : "A data do recebimento não pode ser futura.")]);

            var novoSaldo = divida.EhDivida ? conta.Saldo - parcela.Valor : conta.Saldo + parcela.Valor;

            await uow.Parcelas.RegistrarPagamentoAsync(parcela.Id, dataPagamento.Date);
            await uow.Contas.AtualizarSaldoAsync(conta.Id, novoSaldo);
            await uow.ConfirmarAsync();
        }
    }
}

using ComoEstamos.Data;

namespace ComoEstamos.Core.UseCases
{
    public interface IPagarParcelaUseCase
    {
        /// <summary>
        /// Marca a parcela como paga e ajusta o saldo da conta dela: debita se for
        /// dívida, credita se for crédito (valor a receber).
        /// </summary>
        Task ExecutarAsync(int idParcela, DateTime dataPagamento);
    }

    public class PagarParcelaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IPagarParcelaUseCase
    {
        public async Task ExecutarAsync(int idParcela, DateTime dataPagamento)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var parcela = await uow.Parcelas.ObterPorIdAsync(idParcela)
                ?? throw new KeyNotFoundException($"Parcela {idParcela} não encontrada.");
            if (!parcela.Ativo)
                throw new RegraDeNegocioException("A parcela está desativada.");
            if (parcela.DataPagamento is not null)
                throw new RegraDeNegocioException("A parcela já está paga.");

            var divida = await uow.Dividas.ObterPorIdAsync(parcela.IdDivida)
                ?? throw new KeyNotFoundException($"Divida {parcela.IdDivida} não encontrada.");
            if (!divida.Ativo)
                throw new RegraDeNegocioException("A dívida desta parcela está desativada.");

            var conta = await uow.Contas.ObterPorIdAsync(parcela.IdConta)
                ?? throw new KeyNotFoundException($"Conta {parcela.IdConta} não encontrada.");
            if (!conta.Ativo)
                throw new RegraDeNegocioException("A conta desta parcela está desativada.");

            var novoSaldo = divida.EhDivida ? conta.Saldo - parcela.Valor : conta.Saldo + parcela.Valor;

            await uow.Parcelas.RegistrarPagamentoAsync(parcela.Id, dataPagamento);
            await uow.Contas.AtualizarSaldoAsync(conta.Id, novoSaldo);
            await uow.ConfirmarAsync();
        }
    }
}

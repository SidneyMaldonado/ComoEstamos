using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Parcelas
{
    public interface IPagarParcelaUseCase
    {
        /// <summary>
        /// Marca a parcela como paga (na receita, recebida). O saldo da conta não muda: ele é
        /// atualizado pelo usuário em "Atualizar Saldos". A data não pode ser futura.
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

            if (dataPagamento.Date > DateTime.Today)
                throw new ValidacaoException([new ErroValidacao(nameof(Parcela.DataPagamento), divida.EhDivida ? "A data do pagamento não pode ser futura." : "A data do recebimento não pode ser futura.")]);

            await uow.Parcelas.RegistrarPagamentoAsync(parcela.Id, dataPagamento.Date);
            await uow.ConfirmarAsync();
        }
    }
}

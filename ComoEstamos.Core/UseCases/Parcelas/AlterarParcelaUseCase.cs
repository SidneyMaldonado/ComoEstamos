using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Parcelas
{
    public interface IAlterarParcelaUseCase
    {
        /// <summary>
        /// Valida e grava valor, vencimento e conta de uma parcela em aberto. A dívida passa a ter
        /// como valor total a soma das parcelas, e as descrições são refeitas na ordem de vencimento.
        /// Parcela paga não pode ser alterada.
        /// </summary>
        Task ExecutarAsync(Parcela parcela, int idUsuario);
    }

    public class AlterarParcelaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IAlterarParcelaUseCase
    {
        public async Task ExecutarAsync(Parcela parcela, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var (gravada, divida) = await ParcelaValidador.ObterAtivaDoUsuarioAsync(uow, parcela.Id, idUsuario);
            if (gravada.DataPagamento is not null)
                throw new RegraDeNegocioException("A parcela já está paga e não pode ser alterada.");

            var parcelas = await DividaValidador.ObterParcelasAtivasAsync(uow, divida.Id);
            var somaDasOutras = parcelas.Where(p => p.Id != gravada.Id).Sum(p => p.Valor);
            var validador = await ParcelaValidador.ValidarCamposAsync(uow, parcela, idUsuario, somaDasOutras);
            validador.LancarSeHouverErros();

            gravada.Valor = parcela.Valor;
            gravada.DataVencimento = parcela.DataVencimento.Date;
            gravada.IdConta = parcela.IdConta;
            ParcelaValidador.AtualizarDivida(divida, parcelas);

            await uow.ConfirmarAsync();
        }
    }
}

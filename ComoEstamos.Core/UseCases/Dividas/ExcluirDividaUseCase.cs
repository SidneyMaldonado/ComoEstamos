using ComoEstamos.Data;

namespace ComoEstamos.Core.UseCases.Dividas
{
    public interface IExcluirDividaUseCase
    {
        /// <summary>
        /// Desativa a dívida e todas as parcelas dela, pagas ou não. O saldo das contas não muda:
        /// o que já foi pago continua descontado.
        /// </summary>
        Task ExecutarAsync(int id, int idUsuario);
    }

    public class ExcluirDividaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IExcluirDividaUseCase
    {
        public async Task ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var divida = await DividaValidador.ObterAtivaDoUsuarioAsync(uow, id, idUsuario);
            foreach (var parcela in await DividaValidador.ObterParcelasAtivasAsync(uow, divida.Id))
                parcela.Ativo = false;
            divida.Ativo = false;

            await uow.ConfirmarAsync();
        }
    }
}

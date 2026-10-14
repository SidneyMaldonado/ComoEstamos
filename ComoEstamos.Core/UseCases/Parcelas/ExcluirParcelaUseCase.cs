using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Data;

namespace ComoEstamos.Core.UseCases.Parcelas
{
    public interface IExcluirParcelaUseCase
    {
        /// <summary>
        /// Desativa uma parcela em aberto. A dívida fica com uma parcela a menos e o valor total passa a ser a
        /// soma das restantes; as descrições são refeitas. Parcela paga e a única parcela da dívida não podem
        /// ser excluídas.
        /// </summary>
        Task ExecutarAsync(int id, int idUsuario);
    }

    public class ExcluirParcelaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IExcluirParcelaUseCase
    {
        public async Task ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var (parcela, divida) = await ParcelaValidador.ObterAtivaDoUsuarioAsync(uow, id, idUsuario);
            if (parcela.DataPagamento is not null)
                throw new RegraDeNegocioException("A parcela já está paga e não pode ser excluída.");

            var restantes = (await DividaValidador.ObterParcelasAtivasAsync(uow, divida.Id))
                .Where(p => p.Id != parcela.Id)
                .ToList();
            if (restantes.Count == 0)
                throw new RegraDeNegocioException("Esta é a única parcela da dívida. Para removê-la, exclua a dívida.");

            parcela.Ativo = false;
            ParcelaValidador.AtualizarDivida(divida, restantes);

            await uow.ConfirmarAsync();
        }
    }
}

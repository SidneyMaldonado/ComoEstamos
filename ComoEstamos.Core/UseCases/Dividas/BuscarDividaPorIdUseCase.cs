using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Dividas
{
    public interface IBuscarDividaPorIdUseCase
    {
        /// <summary>
        /// A dívida ativa do usuário, com credor, conta, categoria e as parcelas ativas (por vencimento),
        /// ou <c>null</c> se não existe, é de outro usuário ou foi excluída.
        /// </summary>
        Task<Divida?> ExecutarAsync(int id, int idUsuario);
    }

    public class BuscarDividaPorIdUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarDividaPorIdUseCase
    {
        public async Task<Divida?> ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            var divida = await uow.Dividas.ObterPorIdAsync(id);
            if (divida is not { Ativo: true } || !await DividaValidador.EhDoUsuarioAsync(uow, divida, idUsuario))
                return null;

            if (divida.IdCredor is int idCredor)
                divida.Credor = await uow.Credores.ObterPorIdAsync(idCredor);
            if (divida.IdConta is int idConta)
                divida.Conta = await uow.Contas.ObterPorIdAsync(idConta);
            if (divida.IdCategoria is int idCategoria)
                divida.Categoria = await uow.Categorias.ObterPorIdAsync(idCategoria);
            divida.Parcelas = await uow.Parcelas.ListarPorDividaAsync(divida.Id);
            return divida;
        }
    }
}

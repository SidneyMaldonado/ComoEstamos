using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Parcelas
{
    public interface IBuscarParcelaPorIdUseCase
    {
        /// <summary>
        /// A parcela ativa do usuário, com conta e a dívida (com a categoria e as parcelas ativas dela, por
        /// vencimento, para saber o número da parcela), ou <c>null</c> se não existe, é de outro usuário ou
        /// se a parcela ou a dívida foi excluída.
        /// </summary>
        Task<Parcela?> ExecutarAsync(int id, int idUsuario);
    }

    public class BuscarParcelaPorIdUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IBuscarParcelaPorIdUseCase
    {
        public async Task<Parcela?> ExecutarAsync(int id, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();
            var parcela = await uow.Parcelas.ObterPorIdAsync(id);
            if (parcela is not { Ativo: true })
                return null;
            var divida = await uow.Dividas.ObterPorIdAsync(parcela.IdDivida);
            if (divida is not { Ativo: true } || divida.IdUsuario != idUsuario)
                return null;

            var conta = await uow.Contas.ObterPorIdAsync(parcela.IdConta);
            var categoria = divida.IdCategoria is int idCategoria ? await uow.Categorias.ObterPorIdAsync(idCategoria) : null;
            var parcelas = await uow.Parcelas.ListarPorDividaAsync(divida.Id);

            // Navegações ligadas só depois das consultas: a lista tem outra instância desta parcela.
            parcela.Conta = conta;
            parcela.Divida = divida;
            divida.Categoria = categoria;
            divida.Parcelas = parcelas;
            return parcela;
        }
    }
}

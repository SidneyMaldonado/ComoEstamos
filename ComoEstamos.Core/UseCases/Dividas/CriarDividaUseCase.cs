using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Dividas
{
    public interface ICriarDividaUseCase
    {
        /// <summary>
        /// Cadastra a dívida (ou crédito) do usuário informado e gera suas parcelas.
        /// <see cref="Divida.Valor"/> é o valor total, dividido igualmente entre as parcelas.
        /// </summary>
        /// <returns>A dívida gravada, com <see cref="Divida.Parcelas"/> preenchida.</returns>
        Task<Divida> ExecutarAsync(Divida divida, int idUsuario);
    }

    public class CriarDividaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : ICriarDividaUseCase
    {
        public async Task<Divida> ExecutarAsync(Divida divida, int idUsuario)
        {
            divida.Id = 0;
            divida.Ativo = true;

            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var usuario = await uow.Usuarios.ObterPorIdAsync(idUsuario);
            if (usuario is not { Ativo: true })
                throw new RegraDeNegocioException("Usuário não encontrado ou desativado.");

            (await DividaValidador.ValidarCamposAsync(uow, divida, idUsuario)).LancarSeHouverErros();

            // As parcelas são sempre geradas aqui; o que vier preenchido é descartado.
            divida.Parcelas.Clear();
            divida.DataPrimeiroVencimento = divida.DataPrimeiroVencimento.Date;
            await uow.Dividas.InserirAsync(divida);

            // A dívida ainda não tem Id: as parcelas se ligam a ela pela navegação,
            // e o EF preenche IdDivida e Divida.Parcelas ao gravar.
            foreach (var parcela in Parcelamento.Gerar(divida, primeiroNumero: 1, divida.Valor))
                await uow.Parcelas.InserirAsync(parcela);

            await uow.ConfirmarAsync();
            return divida;
        }
    }
}

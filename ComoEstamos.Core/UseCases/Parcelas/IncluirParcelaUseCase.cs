using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Parcelas
{
    public interface IIncluirParcelaUseCase
    {
        /// <summary>
        /// Acrescenta uma parcela em aberto à dívida <see cref="Parcela.IdDivida"/>, com valor, vencimento, conta e
        /// categoria. A dívida fica com uma parcela a mais, o valor total passa a ser a soma das parcelas e as
        /// descrições são refeitas na ordem de vencimento.
        /// </summary>
        Task<Parcela> ExecutarAsync(Parcela parcela, int idUsuario);
    }

    public class IncluirParcelaUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IIncluirParcelaUseCase
    {
        public async Task<Parcela> ExecutarAsync(Parcela parcela, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var divida = await DividaValidador.ObterAtivaDoUsuarioAsync(uow, parcela.IdDivida, idUsuario);
            var parcelas = await DividaValidador.ObterParcelasAtivasAsync(uow, divida.Id);
            if (parcelas.Count >= DividaValidador.MaximoParcelas)
                throw new RegraDeNegocioException($"A dívida já tem {DividaValidador.MaximoParcelas} parcelas, o máximo permitido.");

            var validador = await ParcelaValidador.ValidarCamposAsync(uow, parcela, idUsuario, parcelas.Sum(p => p.Valor));
            validador.LancarSeHouverErros();

            // Só os campos do formulário: nasce ativa e em aberto, e a descrição vem da renumeração.
            var nova = new Parcela
            {
                IdDivida = divida.Id,
                Valor = parcela.Valor,
                DataVencimento = parcela.DataVencimento.Date,
                IdConta = parcela.IdConta,
                IdCategoria = parcela.IdCategoria
            };
            await uow.Parcelas.InserirAsync(nova);
            ParcelaValidador.AtualizarDivida(divida, [.. parcelas, nova]);

            await uow.ConfirmarAsync();
            return nova;
        }
    }
}

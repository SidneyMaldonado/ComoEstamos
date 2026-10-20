using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.UseCases.Investimentos;
using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Operacoes
{
    public interface IRegistrarOperacaoUseCase
    {
        /// <summary>
        /// Registra a compra ou venda do investimento <see cref="Operacao.IdInvestimento"/> e atualiza a
        /// quantidade dele: soma na compra, subtrai na venda. A cotação não muda. A data (com a hora)
        /// não pode ser futura.
        /// </summary>
        Task<Operacao> ExecutarAsync(Operacao operacao, int idUsuario);
    }

    public class RegistrarOperacaoUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IRegistrarOperacaoUseCase
    {
        public async Task<Operacao> ExecutarAsync(Operacao operacao, int idUsuario)
        {
            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var investimento = await InvestimentoValidador.ObterAtivoDoUsuarioAsync(uow, operacao.IdInvestimento, idUsuario);

            var validador = new ValidadorCampos();
            validador.Numero(nameof(Operacao.Quantidade), "a quantidade", operacao.Quantidade, 6, 0.000001m, Limites.Decimal18x6);
            validador.Numero(nameof(Operacao.ValorTotal), "o valor total", operacao.ValorTotal, 2, 0, Limites.Decimal10x2);
            if (operacao.DataOperacao > DateTime.Now)
                validador.Adicionar(nameof(Operacao.DataOperacao), "A data da operação não pode ser futura.");

            if (!validador.TemErro(nameof(Operacao.Quantidade)))
            {
                if (!operacao.EhCompra && operacao.Quantidade > investimento.Quantidade)
                    validador.Adicionar(nameof(Operacao.Quantidade),
                        $"A quantidade vendida é maior que a disponível ({FormatoBr.Numero(investimento.Quantidade, 6)}).");
                else if (operacao.EhCompra && investimento.Quantidade + operacao.Quantidade > Limites.Decimal18x6)
                    validador.Adicionar(nameof(Operacao.Quantidade), "Com esta compra, a quantidade do investimento passaria do limite.");
            }
            validador.LancarSeHouverErros();

            // Só os campos do formulário: nasce ativa.
            var nova = new Operacao
            {
                IdInvestimento = investimento.Id,
                EhCompra = operacao.EhCompra,
                DataOperacao = operacao.DataOperacao,
                Quantidade = operacao.Quantidade,
                ValorTotal = operacao.ValorTotal
            };
            investimento.Quantidade += nova.EhCompra ? nova.Quantidade : -nova.Quantidade;
            await uow.Operacoes.InserirAsync(nova);

            await uow.ConfirmarAsync();
            return nova;
        }
    }
}

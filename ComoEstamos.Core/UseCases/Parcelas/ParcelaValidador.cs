using ComoEstamos.Core.Formatacao;
using ComoEstamos.Core.UseCases.Dividas;
using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Parcelas
{
    /// <summary>Regras comuns aos casos de uso de parcela (incluir, alterar, excluir e pagar).</summary>
    internal static class ParcelaValidador
    {
        /// <summary>
        /// Parcela e dívida rastreadas pela unidade de trabalho. O dono é conferido pela dívida.
        /// Inexistente ou de outro usuário: <see cref="KeyNotFoundException"/>.
        /// Parcela ou dívida excluída: <see cref="RegraDeNegocioException"/>.
        /// </summary>
        public static async Task<(Parcela Parcela, Divida Divida)> ObterAtivaDoUsuarioAsync(IUnitOfWork uow, int id, int idUsuario)
        {
            var parcela = await uow.Parcelas.ObterPorIdAsync(id);
            var divida = parcela is null ? null : await uow.Dividas.ObterPorIdAsync(parcela.IdDivida);
            if (parcela is null || divida is null || divida.IdUsuario != idUsuario)
                throw new KeyNotFoundException("A parcela não foi encontrada.");
            if (!parcela.Ativo)
                throw new RegraDeNegocioException("A parcela foi excluída.");
            if (!divida.Ativo)
                throw new RegraDeNegocioException("A dívida desta parcela foi excluída.");
            return (parcela, divida);
        }

        /// <summary>
        /// Valida o domínio dos campos editáveis (valor, conta e categoria). O total da dívida —
        /// <paramref name="somaDasOutras"/> mais o valor desta parcela — também não pode passar do limite.
        /// Devolve o validador, para quem chama acrescentar as regras próprias.
        /// </summary>
        public static async Task<ValidadorCampos> ValidarCamposAsync(IUnitOfWork uow, Parcela parcela, int idUsuario, decimal somaDasOutras)
        {
            var validador = new ValidadorCampos();
            validador.Numero(nameof(Parcela.Valor), "o valor da parcela", parcela.Valor, 2, 0.01m, Limites.Decimal10x2);
            if (!validador.TemErro(nameof(Parcela.Valor)) && somaDasOutras + parcela.Valor > Limites.Decimal10x2)
                validador.Adicionar(nameof(Parcela.Valor),
                    $"Com este valor, o total da dívida passaria de {FormatoBr.Moeda(Limites.Decimal10x2)}.");

            var conta = await uow.Contas.ObterPorIdAsync(parcela.IdConta);
            if (conta is not { Ativo: true } || conta.IdUsuario != idUsuario)
                validador.Adicionar(nameof(Parcela.IdConta), parcela.IdConta == 0 ? "Escolha a conta." : "A conta escolhida não existe mais.");

            var categoria = await uow.Categorias.ObterPorIdAsync(parcela.IdCategoria);
            if (categoria is not { Ativo: true } || categoria.IdUsuario != idUsuario)
                validador.Adicionar(nameof(Parcela.IdCategoria), parcela.IdCategoria == 0 ? "Escolha a categoria." : "A categoria escolhida não existe mais.");

            return validador;
        }

        /// <summary>
        /// Depois de incluir, alterar ou excluir uma parcela: o valor total da dívida passa a
        /// ser a soma das parcelas ativas, o número de parcelas a quantidade delas e o primeiro vencimento o
        /// da primeira; as descrições são refeitas na ordem de vencimento (<c>"&lt;Nome&gt; &lt;n&gt;/&lt;total&gt;"</c>).
        /// </summary>
        public static void AtualizarDivida(Divida divida, IReadOnlyCollection<Parcela> ativas)
        {
            // No empate de vencimento, a parcela recém-incluída (ainda sem Id) fica por último.
            var ordenadas = ativas.OrderBy(p => p.DataVencimento).ThenBy(p => p.Id == 0 ? int.MaxValue : p.Id).ToList();
            divida.Valor = ordenadas.Sum(p => p.Valor);
            divida.NumeroParcelas = ordenadas.Count;
            divida.DataPrimeiroVencimento = ordenadas[0].DataVencimento.Date;
            for (var i = 0; i < ordenadas.Count; i++)
                ordenadas[i].Descricao = Parcelamento.Descricao(divida.Nome, i + 1, ordenadas.Count);
        }
    }
}

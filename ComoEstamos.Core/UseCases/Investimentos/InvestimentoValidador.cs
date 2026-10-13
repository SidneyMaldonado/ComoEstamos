using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Investimentos
{
    /// <summary>
    /// Regras comuns aos casos de uso de investimento. O investimento não tem <c>id_usuario</c>:
    /// o dono é o da carteira.
    /// </summary>
    internal static class InvestimentoValidador
    {
        public const int TamanhoMaximoNome = 50;
        public const int TamanhoMaximoObservacao = 500;

        /// <summary>
        /// Normaliza os campos de <paramref name="investimento"/> e valida o domínio de todos eles.
        /// Lança <see cref="ValidacaoException"/> com todos os erros encontrados.
        /// </summary>
        public static async Task ValidarAsync(IUnitOfWork uow, Investimento investimento, int idUsuario)
        {
            investimento.Nome = ValidadorCampos.Normalizar(investimento.Nome);
            investimento.Observacao = ValidadorCampos.NormalizarOpcional(investimento.Observacao);

            var validador = new ValidadorCampos();

            if (investimento.IdCarteira <= 0)
            {
                validador.Adicionar(nameof(Investimento.IdCarteira), "Escolha a carteira.");
            }
            else
            {
                var carteira = await uow.Carteiras.ObterPorIdAsync(investimento.IdCarteira);
                if (carteira is not { Ativo: true } || carteira.IdUsuario != idUsuario)
                    validador.Adicionar(nameof(Investimento.IdCarteira), "A carteira escolhida não existe mais.");
            }

            validador.Texto(nameof(Investimento.Nome), "o nome do investimento", investimento.Nome, TamanhoMaximoNome, obrigatorio: true);
            validador.Numero(nameof(Investimento.Quantidade), "a quantidade", investimento.Quantidade, 6, 0, Limites.Decimal18x6);
            validador.Numero(nameof(Investimento.Cotacao), "a cotação", investimento.Cotacao, 6, 0, Limites.Decimal18x6);
            validador.Texto(nameof(Investimento.Observacao), "a observação", investimento.Observacao, TamanhoMaximoObservacao, obrigatorio: false);

            if (!validador.TemErro(nameof(Investimento.Nome)) && !validador.TemErro(nameof(Investimento.IdCarteira)))
            {
                var existentes = await uow.Investimentos.ListarPorCarteiraAsync(investimento.IdCarteira);
                if (existentes.Any(i => i.Id != investimento.Id && ValidadorCampos.MesmoNome(i.Nome, investimento.Nome)))
                    validador.Adicionar(nameof(Investimento.Nome), "Já existe um investimento com este nome nesta carteira.");
            }

            validador.LancarSeHouverErros();
        }

        /// <summary>
        /// Investimento do usuário, com a carteira carregada, ou <c>null</c> se não existe ou a carteira
        /// é de outro usuário. A entidade fica rastreada pela unidade de trabalho.
        /// </summary>
        public static async Task<Investimento?> ObterDoUsuarioAsync(IUnitOfWork uow, int id, int idUsuario)
        {
            var investimento = await uow.Investimentos.ObterPorIdAsync(id);
            if (investimento is null)
                return null;
            var carteira = await uow.Carteiras.ObterPorIdAsync(investimento.IdCarteira);
            if (carteira is null || carteira.IdUsuario != idUsuario)
                return null;
            investimento.Carteira = carteira;
            return investimento;
        }

        /// <summary>
        /// Investimento rastreado pela unidade de trabalho, para alterar ou excluir.
        /// Inexistente ou de outro usuário: <see cref="KeyNotFoundException"/>. Inativo: <see cref="RegraDeNegocioException"/>.
        /// </summary>
        public static async Task<Investimento> ObterAtivoDoUsuarioAsync(IUnitOfWork uow, int id, int idUsuario)
        {
            var investimento = await ObterDoUsuarioAsync(uow, id, idUsuario)
                ?? throw new KeyNotFoundException("O investimento não foi encontrado.");
            if (!investimento.Ativo)
                throw new RegraDeNegocioException("O investimento foi excluído.");
            return investimento;
        }
    }
}

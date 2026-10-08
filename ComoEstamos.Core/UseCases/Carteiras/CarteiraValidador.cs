using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Carteiras
{
    /// <summary>Regras comuns aos casos de uso de carteira.</summary>
    internal static class CarteiraValidador
    {
        public const int TamanhoMaximoNome = 50;

        /// <summary>
        /// Normaliza os campos de <paramref name="carteira"/> e valida o domínio de todos eles.
        /// Lança <see cref="ValidacaoException"/> com todos os erros encontrados.
        /// </summary>
        public static async Task ValidarAsync(IUnitOfWork uow, Carteira carteira)
        {
            carteira.Nome = ValidadorCampos.Normalizar(carteira.Nome);

            var validador = new ValidadorCampos();
            validador.Texto(nameof(Carteira.Nome), "o nome da carteira", carteira.Nome, TamanhoMaximoNome, obrigatorio: true);

            if (!validador.TemErro(nameof(Carteira.Nome)))
            {
                var existentes = await uow.Carteiras.ListarPorUsuarioAsync(carteira.IdUsuario);
                if (existentes.Any(c => c.Id != carteira.Id && ValidadorCampos.MesmoNome(c.Nome, carteira.Nome)))
                    validador.Adicionar(nameof(Carteira.Nome), "Já existe uma carteira com este nome.");
            }

            validador.LancarSeHouverErros();
        }

        /// <summary>
        /// Carteira rastreada pela unidade de trabalho, para alterar ou excluir.
        /// Inexistente ou de outro usuário: <see cref="KeyNotFoundException"/>. Inativa: <see cref="RegraDeNegocioException"/>.
        /// </summary>
        public static async Task<Carteira> ObterAtivaDoUsuarioAsync(IUnitOfWork uow, int id, int idUsuario)
        {
            var carteira = await uow.Carteiras.ObterPorIdAsync(id);
            if (carteira is null || carteira.IdUsuario != idUsuario)
                throw new KeyNotFoundException("A carteira não foi encontrada.");
            if (!carteira.Ativo)
                throw new RegraDeNegocioException("A carteira foi excluída.");
            return carteira;
        }
    }
}

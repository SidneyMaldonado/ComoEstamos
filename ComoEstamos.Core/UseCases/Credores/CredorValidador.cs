using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Credores
{
    /// <summary>Regras comuns aos casos de uso de credor.</summary>
    internal static class CredorValidador
    {
        public const int TamanhoMaximoNome = 50;

        /// <summary>
        /// Normaliza os campos de <paramref name="credor"/> e valida o domínio de todos eles.
        /// Lança <see cref="ValidacaoException"/> com todos os erros encontrados.
        /// </summary>
        public static async Task ValidarAsync(IUnitOfWork uow, Credor credor)
        {
            credor.Nome = ValidadorCampos.Normalizar(credor.Nome);
            credor.Observacoes = ValidadorCampos.NormalizarOpcional(credor.Observacoes);

            var validador = new ValidadorCampos();
            validador.Texto(nameof(Credor.Nome), "o nome do credor", credor.Nome, TamanhoMaximoNome, obrigatorio: true);
            validador.Texto(nameof(Credor.Observacoes), "as observações", credor.Observacoes, tamanhoMaximo: null, obrigatorio: false);

            if (!validador.TemErro(nameof(Credor.Nome)))
            {
                var existentes = await uow.Credores.ListarPorUsuarioAsync(credor.IdUsuario);
                if (existentes.Any(c => c.Id != credor.Id && ValidadorCampos.MesmoNome(c.Nome, credor.Nome)))
                    validador.Adicionar(nameof(Credor.Nome), "Já existe um credor com este nome.");
            }

            validador.LancarSeHouverErros();
        }

        /// <summary>
        /// Credor rastreado pela unidade de trabalho, para alterar ou excluir.
        /// Inexistente ou de outro usuário: <see cref="KeyNotFoundException"/>. Inativo: <see cref="RegraDeNegocioException"/>.
        /// </summary>
        public static async Task<Credor> ObterAtivoDoUsuarioAsync(IUnitOfWork uow, int id, int idUsuario)
        {
            var credor = await uow.Credores.ObterPorIdAsync(id);
            if (credor is null || credor.IdUsuario != idUsuario)
                throw new KeyNotFoundException("O credor não foi encontrado.");
            if (!credor.Ativo)
                throw new RegraDeNegocioException("O credor foi excluído.");
            return credor;
        }
    }
}

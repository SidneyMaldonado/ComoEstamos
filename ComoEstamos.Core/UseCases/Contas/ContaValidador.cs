using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Contas
{
    /// <summary>Regras comuns aos casos de uso de conta.</summary>
    internal static class ContaValidador
    {
        public const int TamanhoMaximoNome = 100;

        /// <summary>
        /// Normaliza os campos de <paramref name="conta"/> e valida o domínio de todos eles.
        /// Lança <see cref="ValidacaoException"/> com todos os erros encontrados.
        /// </summary>
        public static async Task ValidarAsync(IUnitOfWork uow, Conta conta)
        {
            conta.Nome = ValidadorCampos.Normalizar(conta.Nome);

            var validador = new ValidadorCampos();
            validador.Texto(nameof(Conta.Nome), "o nome da conta", conta.Nome, TamanhoMaximoNome, obrigatorio: true);
            validador.Numero(nameof(Conta.Saldo), "o saldo", conta.Saldo, 2, -Limites.Decimal10x2, Limites.Decimal10x2);

            if (!validador.TemErro(nameof(Conta.Nome)))
            {
                var existentes = await uow.Contas.ListarPorUsuarioAsync(conta.IdUsuario);
                if (existentes.Any(c => c.Id != conta.Id && ValidadorCampos.MesmoNome(c.Nome, conta.Nome)))
                    validador.Adicionar(nameof(Conta.Nome), "Já existe uma conta com este nome.");
            }

            validador.LancarSeHouverErros();
        }

        /// <summary>
        /// Conta rastreada pela unidade de trabalho, para alterar ou excluir.
        /// Inexistente ou de outro usuário: <see cref="KeyNotFoundException"/>. Inativa: <see cref="RegraDeNegocioException"/>.
        /// </summary>
        public static async Task<Conta> ObterAtivaDoUsuarioAsync(IUnitOfWork uow, int id, int idUsuario)
        {
            var conta = await uow.Contas.ObterPorIdAsync(id);
            if (conta is null || conta.IdUsuario != idUsuario)
                throw new KeyNotFoundException("A conta não foi encontrada.");
            if (!conta.Ativo)
                throw new RegraDeNegocioException("A conta foi excluída.");
            return conta;
        }
    }
}

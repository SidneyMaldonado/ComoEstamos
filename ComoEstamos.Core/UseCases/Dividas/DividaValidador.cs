using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Dividas
{
    /// <summary>Regras comuns aos casos de uso de dívida (incluir e alterar).</summary>
    internal static class DividaValidador
    {
        public const int TamanhoMaximoNome = 100;
        public const int MaximoParcelas = 360;

        /// <summary>
        /// Normaliza o nome e valida o domínio de todos os campos editáveis. Devolve o validador com os
        /// erros, para quem chama acrescentar as regras próprias antes de <see cref="ValidadorCampos.LancarSeHouverErros"/>.
        /// </summary>
        public static async Task<ValidadorCampos> ValidarCamposAsync(IUnitOfWork uow, Divida divida, int idUsuario)
        {
            divida.Nome = ValidadorCampos.Normalizar(divida.Nome);

            var validador = new ValidadorCampos();
            validador.Texto(nameof(Divida.Nome), $"o nome da {Tipo(divida)}", divida.Nome, TamanhoMaximoNome, obrigatorio: true);
            validador.Numero(nameof(Divida.Valor), "o valor total", divida.Valor, 2, 0.01m, Limites.Decimal10x2);

            if (divida.NumeroParcelas is < 1 or > MaximoParcelas)
                validador.Adicionar(nameof(Divida.NumeroParcelas), $"O número de parcelas deve estar entre 1 e {MaximoParcelas}.");
            if (divida.DiaVencimento is < 1 or > 31)
                validador.Adicionar(nameof(Divida.DiaVencimento), "O dia de vencimento deve estar entre 1 e 31.");

            if (divida.IdConta is not int idConta)
            {
                validador.Adicionar(nameof(Divida.IdConta), "Escolha a conta.");
            }
            else
            {
                var conta = await uow.Contas.ObterPorIdAsync(idConta);
                if (conta is not { Ativo: true } || conta.IdUsuario != idUsuario)
                    validador.Adicionar(nameof(Divida.IdConta), "A conta escolhida não existe mais.");
            }

            if (divida.IdCategoria is not int idCategoria)
            {
                validador.Adicionar(nameof(Divida.IdCategoria), "Escolha a categoria.");
            }
            else
            {
                var categoria = await uow.Categorias.ObterPorIdAsync(idCategoria);
                if (categoria is not { Ativo: true } || categoria.IdUsuario != idUsuario)
                    validador.Adicionar(nameof(Divida.IdCategoria), "A categoria escolhida não existe mais.");
            }

            if (divida.IdCredor is int idCredor)
            {
                var credor = await uow.Credores.ObterPorIdAsync(idCredor);
                if (credor is not { Ativo: true } || credor.IdUsuario != idUsuario)
                    validador.Adicionar(nameof(Divida.IdCredor),
                        divida.EhDivida ? "O credor escolhido não existe mais." : "O pagador escolhido não existe mais.");
            }

            return validador;
        }

        /// <summary>"dívida" ou "receita" (crédito), para as mensagens.</summary>
        public static string Tipo(Divida divida) => divida.EhDivida ? "dívida" : "receita";

        /// <summary>"pagas" ou "recebidas" (crédito), para as mensagens sobre as parcelas.</summary>
        public static string Pagas(Divida divida) => divida.EhDivida ? "pagas" : "recebidas";

        /// <summary>
        /// Dívida rastreada pela unidade de trabalho, para alterar ou excluir.
        /// Inexistente ou de outro usuário: <see cref="KeyNotFoundException"/>. Inativa: <see cref="RegraDeNegocioException"/>.
        /// </summary>
        public static async Task<Divida> ObterAtivaDoUsuarioAsync(IUnitOfWork uow, int id, int idUsuario)
        {
            var divida = await uow.Dividas.ObterPorIdAsync(id);
            if (divida is null || !await EhDoUsuarioAsync(uow, divida, idUsuario))
                throw new KeyNotFoundException("A dívida não foi encontrada.");
            if (!divida.Ativo)
                throw new RegraDeNegocioException("A dívida foi excluída.");
            return divida;
        }

        /// <summary>A dívida não tem <c>id_usuario</c>: o dono é o da conta dela (a conta é obrigatória).</summary>
        public static async Task<bool> EhDoUsuarioAsync(IUnitOfWork uow, Divida divida, int idUsuario) =>
            divida.IdConta is int idConta && (await uow.Contas.ObterPorIdAsync(idConta))?.IdUsuario == idUsuario;

        /// <summary>
        /// Parcelas ativas da dívida, por vencimento, <b>rastreadas</b> pela unidade de trabalho
        /// (a listagem do repositório não rastreia; cada uma é obtida pelo Id).
        /// </summary>
        public static async Task<List<Parcela>> ObterParcelasAtivasAsync(IUnitOfWork uow, int idDivida)
        {
            var parcelas = new List<Parcela>();
            foreach (var listada in await uow.Parcelas.ListarPorDividaAsync(idDivida))
                parcelas.Add(await uow.Parcelas.ObterPorIdAsync(listada.Id)
                    ?? throw new KeyNotFoundException($"Parcela {listada.Id} não encontrada."));
            return parcelas;
        }
    }
}

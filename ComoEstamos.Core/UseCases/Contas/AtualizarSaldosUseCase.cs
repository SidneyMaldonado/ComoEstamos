using ComoEstamos.Core.Validacao;
using ComoEstamos.Data;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.UseCases.Contas
{
    /// <summary>Saldo informado para uma conta no formulário de atualização de saldos.</summary>
    public record NovoSaldo(int IdConta, decimal Saldo);

    public interface IAtualizarSaldosUseCase
    {
        /// <summary>
        /// Valida todos os saldos e grava, de uma vez, só os que mudaram, registrando cada um em
        /// <c>tb_historico_saldo</c>. Se algum saldo for inválido, nada é gravado. Devolve quantas contas mudaram.
        /// </summary>
        /// <remarks>Os erros de validação vêm com o campo <see cref="CampoSaldo"/> da conta.</remarks>
        Task<int> ExecutarAsync(IReadOnlyList<NovoSaldo> saldos, int idUsuario);
    }

    public class AtualizarSaldosUseCase(IUnitOfWorkFactory unitOfWorkFactory) : IAtualizarSaldosUseCase
    {
        /// <summary>Nome do campo do saldo de uma conta nos erros de validação (<c>Saldo:3</c>).</summary>
        public static string CampoSaldo(int idConta) => $"{nameof(Conta.Saldo)}:{idConta}";

        public async Task<int> ExecutarAsync(IReadOnlyList<NovoSaldo> saldos, int idUsuario)
        {
            if (saldos.DistinctBy(s => s.IdConta).Count() != saldos.Count)
                throw new ArgumentException("A mesma conta foi informada mais de uma vez.", nameof(saldos));

            await using var uow = await unitOfWorkFactory.IniciarAsync();

            var validador = new ValidadorCampos();
            var alteradas = new List<(Conta Conta, decimal Saldo)>();
            foreach (var novo in saldos)
            {
                var conta = await ContaValidador.ObterAtivaDoUsuarioAsync(uow, novo.IdConta, idUsuario);
                validador.Numero(CampoSaldo(conta.Id), "o saldo", novo.Saldo, 2, -Limites.Decimal10x2, Limites.Decimal10x2);
                if (conta.Saldo != novo.Saldo)
                    alteradas.Add((conta, novo.Saldo));
            }
            validador.LancarSeHouverErros();

            var agora = DateTime.Now;
            foreach (var (conta, saldo) in alteradas)
            {
                conta.Saldo = saldo;
                await uow.HistoricosSaldo.InserirAsync(new HistoricoSaldo { Conta = conta, DataHistoricoSaldo = agora, Saldo = saldo });
            }
            await uow.ConfirmarAsync();
            return alteradas.Count;
        }
    }
}

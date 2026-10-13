using ComoEstamos.Core.UseCases.Contas;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.Services
{
    /// <summary>Operações de conta usadas pelas telas, sempre do usuário atual.</summary>
    public interface IContaService
    {
        Task<List<Conta>> BuscarTodasAsync();
        Task<Conta?> BuscarPorIdAsync(int id);
        Task<Conta> IncluirAsync(Conta conta);
        Task AlterarAsync(Conta conta);
        Task ExcluirAsync(int id);

        /// <summary>Grava de uma vez os saldos que mudaram, com histórico. Devolve quantas contas mudaram.</summary>
        Task<int> AtualizarSaldosAsync(IReadOnlyList<NovoSaldo> saldos);
    }

    public class ContaService(
        IUsuarioAtual usuarioAtual,
        IBuscarTodasContasUseCase buscarTodas,
        IBuscarContaPorIdUseCase buscarPorId,
        IIncluirContaUseCase incluir,
        IAlterarContaUseCase alterar,
        IExcluirContaUseCase excluir,
        IAtualizarSaldosUseCase atualizarSaldos) : IContaService
    {
        public async Task<List<Conta>> BuscarTodasAsync() =>
            await buscarTodas.ExecutarAsync(await usuarioAtual.ObterIdAsync());

        public async Task<Conta?> BuscarPorIdAsync(int id) =>
            await buscarPorId.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());

        public async Task<Conta> IncluirAsync(Conta conta) =>
            await incluir.ExecutarAsync(conta, await usuarioAtual.ObterIdAsync());

        public async Task AlterarAsync(Conta conta) =>
            await alterar.ExecutarAsync(conta, await usuarioAtual.ObterIdAsync());

        public async Task ExcluirAsync(int id) =>
            await excluir.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());

        public async Task<int> AtualizarSaldosAsync(IReadOnlyList<NovoSaldo> saldos) =>
            await atualizarSaldos.ExecutarAsync(saldos, await usuarioAtual.ObterIdAsync());
    }
}

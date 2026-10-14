using ComoEstamos.Core.UseCases.Parcelas;
using ComoEstamos.Data.Model;

namespace ComoEstamos.Core.Services
{
    /// <summary>Operações de parcela usadas pelas telas, sempre do usuário atual.</summary>
    public interface IParcelaService
    {
        /// <summary>Parcela com conta, categoria e a dívida (com as parcelas ativas dela).</summary>
        Task<Parcela?> BuscarPorIdAsync(int id);

        /// <summary>Acrescenta uma parcela em aberto à dívida <see cref="Parcela.IdDivida"/>; o total da dívida é recalculado.</summary>
        Task<Parcela> IncluirAsync(Parcela parcela);

        /// <summary>Grava valor, vencimento, conta e categoria de uma parcela em aberto; o total da dívida é recalculado.</summary>
        Task AlterarAsync(Parcela parcela);

        /// <summary>Exclui uma parcela em aberto; a dívida fica com uma parcela a menos.</summary>
        Task ExcluirAsync(int id);

        /// <summary>Registra o pagamento e ajusta o saldo da conta da parcela.</summary>
        Task PagarAsync(int id, DateTime dataPagamento);
    }

    public class ParcelaService(
        IUsuarioAtual usuarioAtual,
        IBuscarParcelaPorIdUseCase buscarPorId,
        IIncluirParcelaUseCase incluir,
        IAlterarParcelaUseCase alterar,
        IExcluirParcelaUseCase excluir,
        IPagarParcelaUseCase pagar) : IParcelaService
    {
        public async Task<Parcela?> BuscarPorIdAsync(int id) =>
            await buscarPorId.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());

        public async Task<Parcela> IncluirAsync(Parcela parcela) =>
            await incluir.ExecutarAsync(parcela, await usuarioAtual.ObterIdAsync());

        public async Task AlterarAsync(Parcela parcela) =>
            await alterar.ExecutarAsync(parcela, await usuarioAtual.ObterIdAsync());

        public async Task ExcluirAsync(int id) =>
            await excluir.ExecutarAsync(id, await usuarioAtual.ObterIdAsync());

        public async Task PagarAsync(int id, DateTime dataPagamento) =>
            await pagar.ExecutarAsync(id, dataPagamento, await usuarioAtual.ObterIdAsync());
    }
}
